using Mediator;
using Microsoft.Extensions.Options;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.ScheduledJobs.Models;
using Worker.Services;
using SharedServices = skestock.Shared.Services;

namespace Worker.Statistics;

/// <summary>
/// Runs the daily statistics job once per scheduled occurrence (default 21:00 business time).
/// </summary>
/// <remarks>
/// Each loop iteration:
/// <list type="number">
///   <item>Finds the most recent occurrence that is due and loads the persisted run state.</item>
///   <item>If nothing is due, sleeps until the pending retry or the next occurrence.</item>
///   <item>Otherwise runs the job under a distributed lock (only one Worker instance runs it),
///   retrying failures per <see cref="DailyStatisticsRetryPolicy"/>.</item>
/// </list>
/// The run state is persisted before and after every attempt, so a restarted Worker catches
/// up a missed occurrence and resumes the remaining retries instead of starting over.
/// </remarks>
public class DailyStatisticsService(
    IOptions<DailyStatisticsOptions> options,
    IServiceScopeFactory scopeFactory,
    IDistributedLock distributedLock,
    TimeProvider timeProvider,
    ILogger<DailyStatisticsService> logger) : BackgroundService
{
    // Must exceed the longest expected run, otherwise a second Worker could start in parallel.
    private static readonly TimeSpan LockExpiry = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan LockRetryDelay = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan ErrorLoopDelay = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan MinimumScheduleDelay = TimeSpan.FromSeconds(1);

    private readonly DailyStatisticsOptions _options = options.Value;
    private readonly DailyScheduleCalculator _schedule = new(options.Value);
    private readonly DailyStatisticsRunStore _runStore = new(scopeFactory);
    private readonly DailyStatisticsJob _job = new(
        options.Value,
        TimeZoneInfo.FindSystemTimeZoneById(options.Value.TimeZone),
        timeProvider,
        logger);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "Daily statistics service started. Schedule: {RunAt} ({TimeZone}).",
            _options.RunAt,
            _options.TimeZone);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var outcome = await ProcessDueOccurrenceAsync(stoppingToken);
                if (outcome == ScheduledRunOutcome.LockNotAcquired)
                {
                    await DelayAsync(LockRetryDelay, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                // Keeps the hosted service alive on unexpected errors (e.g. database unavailable).
                logger.LogError(ex, "Daily statistics scheduling loop failed.");
                await DelayAsync(ErrorLoopDelay, stoppingToken);
            }
        }
    }

    /// <summary>The actual job work. Virtual so tests can replace it.</summary>
    protected virtual Task ExecuteJobAsync(
        ISender sender,
        DailyStatisticsJobContext context,
        CancellationToken cancellationToken) =>
        _job.RunAsync(sender, context, cancellationToken);

    /// <summary>Time-based waiting. Virtual so tests can observe delays without waiting.</summary>
    protected virtual Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken) =>
        Task.Delay(delay, timeProvider, cancellationToken);

    private async Task<ScheduledRunOutcome> ProcessDueOccurrenceAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var occurrence = _schedule.GetMostRecentDueOccurrence(now);
        var lastRun = await _runStore.GetLastRunAsync(cancellationToken);

        if (!DailyStatisticsRunPolicy.ShouldRun(lastRun, occurrence, now, allowRetry: false))
        {
            await WaitForNextActionAsync(now, lastRun, occurrence, cancellationToken);
            return ScheduledRunOutcome.Completed;
        }

        return await RunWithRetriesAsync(occurrence, lastRun, cancellationToken);
    }

    private async Task<ScheduledRunOutcome> RunWithRetriesAsync(
        DateTimeOffset occurrence,
        ScheduledJobRunDto? lastRun,
        CancellationToken cancellationToken)
    {
        var firstAttemptIndex = DailyStatisticsRunPolicy.GetNextAttemptIndex(lastRun, occurrence);

        for (var attemptIndex = firstAttemptIndex;
             attemptIndex <= DailyStatisticsRetryPolicy.LastAttemptIndex;
             attemptIndex++)
        {
            try
            {
                return await TryRunAttemptAsync(occurrence, attemptIndex, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex) when (!DailyStatisticsRetryPolicy.IsLastAttempt(attemptIndex))
            {
                var retryDelay = DailyStatisticsRetryPolicy.GetRetryDelay(attemptIndex);
                logger.LogWarning(
                    ex,
                    "Daily statistics run attempt {AttemptNumber} failed; retrying in {RetryDelay}.",
                    attemptIndex + 1,
                    retryDelay);
                await DelayAsync(retryDelay, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Daily statistics run failed after {AttemptCount} attempts.",
                    attemptIndex + 1);
                await TryRecordFinalFailureAsync(ex, attemptIndex + 1, cancellationToken);
                return ScheduledRunOutcome.Failed;
            }
        }

        // Unreachable: the loop always returns on its last attempt.
        return ScheduledRunOutcome.Failed;
    }

    // Runs a single attempt while holding the distributed lock. Job and persistence failures
    // propagate to the retry loop.
    private async Task<ScheduledRunOutcome> TryRunAttemptAsync(
        DateTimeOffset occurrence,
        int attemptIndex,
        CancellationToken cancellationToken)
    {
        await using var lease = await distributedLock.TryAcquireAsync(
            SharedServices.DailyStatisticsLockKey,
            LockExpiry,
            cancellationToken);

        if (lease is null)
        {
            logger.LogInformation(
                "Daily statistics run for {Occurrence} was skipped because another Worker owns the lock.",
                occurrence);
            return ScheduledRunOutcome.LockNotAcquired;
        }

        // Re-read under the lock: another Worker may have finished this occurrence meanwhile.
        var lastRun = await _runStore.GetLastRunAsync(cancellationToken);
        if (!DailyStatisticsRunPolicy.ShouldRun(
                lastRun,
                occurrence,
                timeProvider.GetUtcNow(),
                allowRetry: attemptIndex > 0))
        {
            return ScheduledRunOutcome.Completed;
        }

        var context = new DailyStatisticsJobContext(occurrence, lastRun?.LastSucceededAt);
        await RunAndRecordAttemptAsync(context, attemptIndex, cancellationToken);
        return ScheduledRunOutcome.Completed;
    }

    // Persists Running -> Succeeded/Failed around the job. Recording success is inside the try
    // block on purpose: if it fails, the attempt is recorded (and retried) as failed.
    private async Task RunAndRecordAttemptAsync(
        DailyStatisticsJobContext context,
        int attemptIndex,
        CancellationToken cancellationToken)
    {
        var attemptNumber = attemptIndex + 1;
        var attemptedAt = timeProvider.GetUtcNow();
        await _runStore.MarkRunningAsync(attemptedAt, attemptNumber, cancellationToken);

        try
        {
            await RunJobInNewScopeAsync(context, cancellationToken);
            await _runStore.MarkSucceededAsync(
                attemptedAt,
                timeProvider.GetUtcNow(),
                attemptNumber,
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            var failedAt = timeProvider.GetUtcNow();
            await _runStore.MarkFailedAsync(
                failedAt,
                attemptNumber,
                ex.Message,
                DailyStatisticsRetryPolicy.GetNextRetryAt(failedAt, attemptIndex),
                cancellationToken);
            throw;
        }
    }

    private async Task RunJobInNewScopeAsync(
        DailyStatisticsJobContext context,
        CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();

        // Scheduled work has no end user: run as the anonymous system identity.
        var ambientUser = scope.ServiceProvider.GetRequiredService<AmbientUser>();
        ambientUser.Id = null;
        ambientUser.Roles = [];

        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        logger.LogInformation(
            "Daily statistics run started for scheduled occurrence {Occurrence}.",
            context.Occurrence);
        await ExecuteJobAsync(sender, context, cancellationToken);
        logger.LogInformation(
            "Daily statistics run finished for scheduled occurrence {Occurrence}.",
            context.Occurrence);
    }

    // The last attempt normally records its own failure. This covers failures that happened
    // before or while that state was written (lock, database, or record errors), so the
    // exhausted state is not lost. It is best-effort: its own failure is only logged.
    private async Task TryRecordFinalFailureAsync(
        Exception exception,
        int attemptCount,
        CancellationToken cancellationToken)
    {
        try
        {
            await _runStore.MarkFailedAsync(
                timeProvider.GetUtcNow(),
                attemptCount,
                exception.Message,
                nextRetryAtUtc: null,
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception recordException)
        {
            logger.LogWarning(recordException, "Could not persist the final daily statistics failure state.");
        }
    }

    private async Task WaitForNextActionAsync(
        DateTimeOffset now,
        ScheduledJobRunDto? lastRun,
        DateTimeOffset occurrence,
        CancellationToken cancellationToken)
    {
        if (DailyStatisticsRunPolicy.GetPendingRetryAt(lastRun, occurrence, now) is { } nextRetryAt)
        {
            logger.LogInformation("Next daily statistics retry is scheduled for {NextRetryAt}.", nextRetryAt);
            await DelayAsync(nextRetryAt - now, cancellationToken);
            return;
        }

        var nextOccurrence = _schedule.GetNextOccurrence(now);
        var delay = nextOccurrence - now;

        logger.LogInformation("Next daily statistics run is scheduled for {NextOccurrence}.", nextOccurrence);
        await DelayAsync(delay > TimeSpan.Zero ? delay : MinimumScheduleDelay, cancellationToken);
    }
}

using Mediator;
using FluentResults;
using Microsoft.Extensions.Options;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.ScheduledJobs.Commands.RecordScheduledJobRun;
using skestock.Application.Features.ScheduledJobs.Models;
using skestock.Application.Features.ScheduledJobs.Queries.GetScheduledJobLastRun;
using skestock.Application.Features.Statistics.Commands.MaterializeDailyConsumption;
using skestock.Application.Features.Statistics.Commands.MaterializePurchaseStatistics;
using skestock.Application.Features.Statistics.Queries.GetConsumptionBackfillStart;
using skestock.Domain.Enums;
using Worker.Services;
using SharedServices = skestock.Shared.Services;

namespace Worker.Statistics;

public class DailyStatisticsService(
    IOptions<DailyStatisticsOptions> options,
    IServiceScopeFactory scopeFactory,
    IDistributedLock distributedLock,
    TimeProvider timeProvider,
    ILogger<DailyStatisticsService> logger) : BackgroundService
{
    private static readonly TimeSpan LockExpiry = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan LockRetryDelay = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan ErrorLoopDelay = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan[] RetryDelays =
    [
        TimeSpan.FromMinutes(1),
        TimeSpan.FromMinutes(5),
        TimeSpan.FromMinutes(15)
    ];

    private static int MaxAttempts => RetryDelays.Length + 1;

    private readonly DailyScheduleCalculator _schedule = new(options.Value);
    private readonly TimeZoneInfo _timeZone = TimeZoneInfo.FindSystemTimeZoneById(options.Value.TimeZone);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "Daily statistics service started. Schedule: {RunAt} ({TimeZone}).",
            options.Value.RunAt,
            options.Value.TimeZone);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var outcome = await ProcessScheduleAsync(stoppingToken);
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
                logger.LogError(ex, "Daily statistics scheduling loop failed.");
                await DelayAsync(ErrorLoopDelay, stoppingToken);
            }
        }
    }

    protected virtual async Task ExecuteJobAsync(
        ISender sender,
        DailyStatisticsJobContext context,
        CancellationToken cancellationToken)
    {
        var (fromDate, toDate) = ConsumptionRangeCalculator.Calculate(
            timeProvider.GetUtcNow(),
            context.LastSucceededAtUtc,
            _timeZone,
            options.Value.MaxBackfillDays);

        // Existing history older than the regular range (e.g. on first publish) is backfilled once;
        // an interrupted backfill leaves the gap in place, so the next run resumes it.
        var backfillStart = await sender.Send(
            new GetConsumptionBackfillStartQuery { TimeZoneId = options.Value.TimeZone },
            cancellationToken);
        EnsureSuccess(backfillStart);
        if (backfillStart.Value is { } historyStart && historyStart < fromDate)
        {
            logger.LogInformation(
                "Backfilling daily consumption history from {FromDate}.",
                historyStart);
            fromDate = historyStart;
        }

        foreach (var (windowFrom, windowTo) in ConsumptionRangeCalculator.SplitIntoWindows(
                     fromDate,
                     toDate,
                     MaterializeDailyConsumptionCommand.MaxDays))
        {
            var result = await sender.Send(
                new MaterializeDailyConsumptionCommand
                {
                    FromDate = windowFrom,
                    ToDate = windowTo,
                    TimeZoneId = options.Value.TimeZone
                },
                cancellationToken);

            EnsureSuccess(result);
            logger.LogInformation(
                "Materialized {RowCount} daily consumption rows for {FromDate}..{ToDate}.",
                result.Value,
                windowFrom,
                windowTo);
        }

        await MaterializePurchaseStatisticsAsync(sender, cancellationToken);
    }

    // Purchase statistics are secondary: a failure is logged without failing the consumption job.
    private async Task MaterializePurchaseStatisticsAsync(ISender sender, CancellationToken cancellationToken)
    {
        try
        {
            var result = await sender.Send(
                new MaterializePurchaseStatisticsCommand { TimeZoneId = options.Value.TimeZone },
                cancellationToken);

            if (result.IsFailed)
            {
                logger.LogError(
                    "Purchase statistics materialization failed: {Errors}",
                    string.Join("; ", result.Errors.Select(error => error.Message)));
                return;
            }

            logger.LogInformation("Materialized {RowCount} purchase statistics rows.", result.Value);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            logger.LogError(ex, "Purchase statistics materialization failed.");
        }
    }

    protected virtual Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken) =>
        Task.Delay(delay, timeProvider, cancellationToken);

    private async Task<ScheduledRunOutcome> ProcessScheduleAsync(
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var occurrence = _schedule.GetMostRecentDueOccurrence(now);
        var lastRun = await GetLastRunAsync(cancellationToken);

        if (!ShouldRun(lastRun, occurrence, now, allowRetry: false))
        {
            await DelayUntilNextActionAsync(now, lastRun, occurrence, cancellationToken);
            return ScheduledRunOutcome.Completed;
        }

        return await RunWithRetriesAsync(occurrence, lastRun, cancellationToken);
    }

    private async Task<ScheduledRunOutcome> RunWithRetriesAsync(
        DateTimeOffset occurrence,
        ScheduledJobRunDto? lastRun,
        CancellationToken cancellationToken)
    {
        var firstAttempt = GetNextAttemptIndex(lastRun, occurrence);
        for (var attempt = firstAttempt; attempt <= RetryDelays.Length; attempt++)
        {
            try
            {
                var outcome = await TryRunOnceAsync(
                    occurrence,
                    attempt,
                    cancellationToken);

                if (outcome == ScheduledRunOutcome.LockNotAcquired)
                {
                    return outcome;
                }

                return outcome;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                if (attempt == RetryDelays.Length)
                {
                    logger.LogError(
                        ex,
                        "Daily statistics run failed after {AttemptCount} attempts.",
                        attempt + 1);
                    await TryRecordFailureAsync(ex, attempt + 1, cancellationToken);
                    return ScheduledRunOutcome.Failed;
                }

                logger.LogWarning(
                    ex,
                    "Daily statistics run attempt {AttemptNumber} failed; retrying in {RetryDelay}.",
                    attempt + 1,
                    RetryDelays[attempt]);
                await DelayAsync(RetryDelays[attempt], cancellationToken);
            }
        }

        return ScheduledRunOutcome.Failed;
    }

    private async Task TryRecordFailureAsync(
        Exception exception,
        int attemptCount,
        CancellationToken cancellationToken)
    {
        try
        {
            await RecordRunAsync(
                timeProvider.GetUtcNow(),
                succeededAtUtc: null,
                error: exception.Message,
                status: ScheduledJobRunStatus.Failed,
                attemptCount: attemptCount,
                nextRetryAtUtc: null,
                cancellationToken: cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception recordException)
        {
            logger.LogWarning(
                recordException,
                "Could not persist the final daily statistics failure state.");
        }
    }

    private async Task<ScheduledRunOutcome> TryRunOnceAsync(
        DateTimeOffset occurrence,
        int attempt,
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

        var lastRun = await GetLastRunAsync(cancellationToken);
        if (!ShouldRun(
                lastRun,
                occurrence,
                timeProvider.GetUtcNow(),
                allowRetry: attempt > 0))
        {
            return ScheduledRunOutcome.Completed;
        }

        var attemptedAt = timeProvider.GetUtcNow();
        await RecordRunAsync(
            attemptedAt,
            succeededAtUtc: null,
            error: null,
            status: ScheduledJobRunStatus.Running,
            attemptCount: attempt + 1,
            nextRetryAtUtc: null,
            cancellationToken: cancellationToken);

        try
        {
            using var scope = scopeFactory.CreateScope();
            var ambientUser = scope.ServiceProvider.GetRequiredService<AmbientUser>();
            ambientUser.Id = null;
            ambientUser.Roles = [];

            var sender = scope.ServiceProvider.GetRequiredService<ISender>();

            logger.LogInformation(
                "Daily statistics run started for scheduled occurrence {Occurrence}.",
                occurrence);
            await ExecuteJobAsync(
                sender,
                new DailyStatisticsJobContext(occurrence, lastRun?.LastSucceededAt),
                cancellationToken);
            logger.LogInformation(
                "Daily statistics run finished for scheduled occurrence {Occurrence}.",
                occurrence);

            await RecordRunAsync(
                attemptedAt,
                timeProvider.GetUtcNow(),
                error: null,
                status: ScheduledJobRunStatus.Succeeded,
                attemptCount: attempt + 1,
                nextRetryAtUtc: null,
                cancellationToken: cancellationToken);
            return ScheduledRunOutcome.Completed;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            var failedAt = timeProvider.GetUtcNow();
            DateTimeOffset? nextRetryAtUtc = attempt < RetryDelays.Length
                ? failedAt + RetryDelays[attempt]
                : null;
            await RecordRunAsync(
                failedAt,
                succeededAtUtc: null,
                error: ex.Message,
                status: ScheduledJobRunStatus.Failed,
                attemptCount: attempt + 1,
                nextRetryAtUtc: nextRetryAtUtc,
                cancellationToken: cancellationToken);
            throw;
        }
    }

    private async Task<ScheduledJobRunDto?> GetLastRunAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var result = await sender.Send(
            new GetScheduledJobLastRunQuery { JobName = SharedServices.DailyStatisticsJobName },
            cancellationToken);

        EnsureSuccess(result);
        return result.Value;
    }

    private async Task RecordRunAsync(
        DateTimeOffset attemptedAtUtc,
        DateTimeOffset? succeededAtUtc,
        string? error,
        ScheduledJobRunStatus status,
        int attemptCount,
        DateTimeOffset? nextRetryAtUtc,
        CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var result = await sender.Send(
            new RecordScheduledJobRunCommand
            {
                JobName = SharedServices.DailyStatisticsJobName,
                AttemptedAtUtc = attemptedAtUtc,
                SucceededAtUtc = succeededAtUtc,
                Error = error,
                Status = status,
                AttemptCount = attemptCount,
                NextRetryAtUtc = nextRetryAtUtc
            },
            cancellationToken);

        EnsureSuccess(result);
    }

    private async Task DelayUntilNextActionAsync(
        DateTimeOffset now,
        ScheduledJobRunDto? lastRun,
        DateTimeOffset occurrence,
        CancellationToken cancellationToken)
    {
        if (lastRun is
            {
                Status: ScheduledJobRunStatus.Failed,
                NextRetryAt: { } nextRetryAt,
                LastAttemptAt: { } lastAttemptAt
            }
            && lastAttemptAt >= occurrence
            && lastRun.AttemptCount < MaxAttempts
            && nextRetryAt > now)
        {
            logger.LogInformation(
                "Next daily statistics retry is scheduled for {NextRetryAt}.",
                nextRetryAt);
            await DelayAsync(nextRetryAt - now, cancellationToken);
            return;
        }

        await DelayUntilNextOccurrenceAsync(now, cancellationToken);
    }

    private async Task DelayUntilNextOccurrenceAsync(
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var nextOccurrence = _schedule.GetNextOccurrence(now);
        var delay = nextOccurrence - now;
        if (delay <= TimeSpan.Zero)
        {
            delay = TimeSpan.FromSeconds(1);
        }

        logger.LogInformation(
            "Next daily statistics run is scheduled for {NextOccurrence}.",
            nextOccurrence);
        await DelayAsync(delay, cancellationToken);
    }

    private static bool ShouldRun(
        ScheduledJobRunDto? lastRun,
        DateTimeOffset occurrence,
        DateTimeOffset now,
        bool allowRetry)
    {
        if (lastRun is null)
        {
            return true;
        }

        if (lastRun.LastSucceededAt is { } succeededAt && succeededAt >= occurrence)
        {
            return false;
        }

        if (lastRun.LastAttemptAt is not { } attemptedAt || attemptedAt < occurrence)
        {
            return true;
        }

        if (lastRun.AttemptCount >= MaxAttempts ||
            lastRun.Status == ScheduledJobRunStatus.Succeeded)
        {
            return false;
        }

        if (lastRun.Status == ScheduledJobRunStatus.Running || allowRetry)
        {
            return true;
        }

        return lastRun.Status == ScheduledJobRunStatus.Failed &&
            lastRun.NextRetryAt is { } nextRetryAt &&
            nextRetryAt <= now;
    }

    private static int GetNextAttemptIndex(
        ScheduledJobRunDto? lastRun,
        DateTimeOffset occurrence)
    {
        if (lastRun?.LastAttemptAt is not { } attemptedAt || attemptedAt < occurrence)
        {
            return 0;
        }

        return Math.Clamp(lastRun.AttemptCount, 0, RetryDelays.Length);
    }

    private static void EnsureSuccess(IResultBase result)
    {
        if (result.IsSuccess)
        {
            return;
        }

        throw new InvalidOperationException(
            string.Join("; ", result.Errors.Select(error => error.Message)));
    }

    private enum ScheduledRunOutcome
    {
        Completed,
        Failed,
        LockNotAcquired
    }
}

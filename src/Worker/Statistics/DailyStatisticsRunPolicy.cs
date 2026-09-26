using skestock.Application.Features.ScheduledJobs.Models;
using skestock.Domain.Enums;

namespace Worker.Statistics;

/// <summary>
/// Pure decisions about whether the daily statistics job must run for a scheduled occurrence,
/// based on the persisted state of the previous run.
/// </summary>
/// <remarks>
/// The persisted <see cref="ScheduledJobRunDto"/> only describes the most recent attempt. An
/// attempt "belongs" to an occurrence when it started at or after that occurrence; anything
/// older is state from a previous day and is ignored.
/// </remarks>
public static class DailyStatisticsRunPolicy
{
    /// <summary>
    /// Whether the job should run now for <paramref name="occurrence"/>.
    /// </summary>
    /// <param name="allowRetry">
    /// <see langword="true"/> when the caller is already inside its own retry loop and does not
    /// need to wait for the persisted <see cref="ScheduledJobRunDto.NextRetryAt"/>.
    /// </param>
    public static bool ShouldRun(
        ScheduledJobRunDto? lastRun,
        DateTimeOffset occurrence,
        DateTimeOffset utcNow,
        bool allowRetry)
    {
        if (lastRun is null)
        {
            return true;
        }

        if (HasSucceededSince(lastRun, occurrence))
        {
            return false;
        }

        if (!HasAttemptedSince(lastRun, occurrence))
        {
            return true;
        }

        // From here on, the last attempt belongs to this occurrence.
        if (lastRun.Status == ScheduledJobRunStatus.Succeeded ||
            lastRun.AttemptCount >= DailyStatisticsRetryPolicy.MaxAttempts)
        {
            return false;
        }

        // "Running" means a previous Worker stopped mid-run without recording an outcome
        // (crash/restart after its lock expired), so the attempt is resumed immediately.
        if (lastRun.Status == ScheduledJobRunStatus.Running || allowRetry)
        {
            return true;
        }

        return IsRetryDue(lastRun, utcNow);
    }

    /// <summary>
    /// Zero-based index of the next attempt for <paramref name="occurrence"/>. A restarted
    /// Worker resumes the remaining retries instead of starting a fresh retry cycle.
    /// </summary>
    public static int GetNextAttemptIndex(ScheduledJobRunDto? lastRun, DateTimeOffset occurrence)
    {
        if (lastRun is null || !HasAttemptedSince(lastRun, occurrence))
        {
            return 0;
        }

        // AttemptCount is one-based, so it is already the index of the following attempt.
        return Math.Clamp(lastRun.AttemptCount, 0, DailyStatisticsRetryPolicy.LastAttemptIndex);
    }

    /// <summary>
    /// The instant of a retry that is scheduled for this occurrence but not yet due, or
    /// <see langword="null"/> when there is nothing to wait for besides the next occurrence.
    /// </summary>
    public static DateTimeOffset? GetPendingRetryAt(
        ScheduledJobRunDto? lastRun,
        DateTimeOffset occurrence,
        DateTimeOffset utcNow)
    {
        if (lastRun is not { Status: ScheduledJobRunStatus.Failed, NextRetryAt: { } nextRetryAt } ||
            !HasAttemptedSince(lastRun, occurrence) ||
            lastRun.AttemptCount >= DailyStatisticsRetryPolicy.MaxAttempts)
        {
            return null;
        }

        return nextRetryAt > utcNow ? nextRetryAt : null;
    }

    private static bool HasSucceededSince(ScheduledJobRunDto lastRun, DateTimeOffset occurrence) =>
        lastRun.LastSucceededAt is { } succeededAt && succeededAt >= occurrence;

    private static bool HasAttemptedSince(ScheduledJobRunDto lastRun, DateTimeOffset occurrence) =>
        lastRun.LastAttemptAt is { } attemptedAt && attemptedAt >= occurrence;

    private static bool IsRetryDue(ScheduledJobRunDto lastRun, DateTimeOffset utcNow) =>
        lastRun is { Status: ScheduledJobRunStatus.Failed, NextRetryAt: { } nextRetryAt } &&
        nextRetryAt <= utcNow;
}

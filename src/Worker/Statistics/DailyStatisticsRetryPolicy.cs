namespace Worker.Statistics;

/// <summary>
/// Retry schedule for one scheduled occurrence of the daily statistics job.
/// </summary>
/// <remarks>
/// Attempts are identified by a zero-based <c>attemptIndex</c>; the persisted
/// <c>AttemptCount</c> is the one-based attempt number (<c>attemptIndex + 1</c>).
/// Attempt <c>n</c> is followed by <see cref="RetryDelays"/>[<c>n</c>] when it fails, so there
/// is one more attempt than there are delays.
/// </remarks>
public static class DailyStatisticsRetryPolicy
{
    private static readonly TimeSpan[] RetryDelays =
    [
        TimeSpan.FromMinutes(1),
        TimeSpan.FromMinutes(5),
        TimeSpan.FromMinutes(15)
    ];

    /// <summary>Total number of attempts allowed per scheduled occurrence.</summary>
    public static int MaxAttempts => RetryDelays.Length + 1;

    /// <summary>Zero-based index of the final attempt.</summary>
    public static int LastAttemptIndex => RetryDelays.Length;

    /// <summary>Whether no retry follows the attempt with the given zero-based index.</summary>
    public static bool IsLastAttempt(int attemptIndex) => attemptIndex >= LastAttemptIndex;

    /// <summary>Delay to wait after the attempt with the given zero-based index fails.</summary>
    public static TimeSpan GetRetryDelay(int attemptIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(attemptIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(attemptIndex, RetryDelays.Length);

        return RetryDelays[attemptIndex];
    }

    /// <summary>
    /// When the next retry is due after the given attempt failed, or <see langword="null"/>
    /// when the failed attempt was the last one.
    /// </summary>
    public static DateTimeOffset? GetNextRetryAt(DateTimeOffset failedAtUtc, int attemptIndex) =>
        IsLastAttempt(attemptIndex) ? null : failedAtUtc + GetRetryDelay(attemptIndex);
}

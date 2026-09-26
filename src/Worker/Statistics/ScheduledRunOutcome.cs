namespace Worker.Statistics;

internal enum ScheduledRunOutcome
{
    /// <summary>The occurrence was handled (succeeded, or nothing was left to do).</summary>
    Completed,

    /// <summary>Every allowed attempt for the occurrence failed.</summary>
    Failed,

    /// <summary>Another Worker instance holds the job lock; try again shortly.</summary>
    LockNotAcquired
}

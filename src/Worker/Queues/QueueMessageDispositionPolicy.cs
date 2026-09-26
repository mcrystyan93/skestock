namespace Worker.Queues;

/// <summary>
/// Maps a processing status to a transport action. Pure, so the retry/poison rules can be
/// tested without Azure Queue clients.
/// </summary>
public static class QueueMessageDispositionPolicy
{
    /// <summary>Deliveries after which a still-failing message is given up on.</summary>
    public const int MaxDequeueCount = 5;

    /// <param name="dequeueCount">How many times Azure Queue has delivered this message, including now.</param>
    public static QueueMessageDisposition Decide(QueueMessageProcessingStatus status, long dequeueCount) =>
        status switch
        {
            QueueMessageProcessingStatus.Succeeded or QueueMessageProcessingStatus.Duplicate =>
                QueueMessageDisposition.Delete,

            QueueMessageProcessingStatus.PermanentFailure =>
                QueueMessageDisposition.MoveToPoison,

            QueueMessageProcessingStatus.RetryableFailure when dequeueCount >= MaxDequeueCount =>
                QueueMessageDisposition.MoveToPoison,

            QueueMessageProcessingStatus.RetryableFailure =>
                QueueMessageDisposition.Retry,

            _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown processing status.")
        };
}

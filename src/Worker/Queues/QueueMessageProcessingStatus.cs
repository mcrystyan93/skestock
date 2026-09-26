namespace Worker.Queues;

/// <summary>Business outcome of processing one queue message.</summary>
public enum QueueMessageProcessingStatus
{
    /// <summary>The request was dispatched and its changes were committed.</summary>
    Succeeded,

    /// <summary>The message was already handled (here or by another instance); nothing to do.</summary>
    Duplicate,

    /// <summary>A later delivery might succeed (e.g. a dependency was temporarily unavailable).</summary>
    RetryableFailure,

    /// <summary>Retrying can never succeed (malformed message, validation, failed result...).</summary>
    PermanentFailure
}

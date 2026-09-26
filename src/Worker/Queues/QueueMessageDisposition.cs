namespace Worker.Queues;

/// <summary>What the polling module does with a received message after processing it.</summary>
public enum QueueMessageDisposition
{
    /// <summary>Acknowledge: delete the message from the source queue.</summary>
    Delete,

    /// <summary>
    /// Leave the message in place. Azure Queue makes it visible again after the visibility
    /// timeout and increments its dequeue count, which acts as the retry counter.
    /// </summary>
    Retry,

    /// <summary>Copy the raw message to the poison queue, then delete it from the source queue.</summary>
    MoveToPoison
}

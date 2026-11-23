namespace skestock.Application.Queues;

/// <summary>
/// Per-message delivery facts the queue consumer shares with handlers. Outside a queue consumer
/// (e.g. a web request) it keeps its defaults.
/// </summary>
public sealed class MessageDeliveryContext
{
    /// <summary>True when a failure now will not be retried: the message goes to the poison queue.</summary>
    public bool IsFinalAttempt { get; set; }
}

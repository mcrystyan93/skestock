using skestock.Domain.Queues;

namespace Worker.Queues;

public enum QueueMessageProcessingStatus
{
    Succeeded,
    Duplicate,
    RetryableFailure,
    PermanentFailure
}

/// <summary>
/// Describes what the polling module should do after the business module handled a message.
/// Keeping this result transport-neutral makes processing rules easy to test without Azure Queue
/// clients.
/// </summary>
public sealed record QueueMessageProcessingResult(
    QueueMessageProcessingStatus Status,
    MessageEnvelope? Envelope = null)
{
    public static QueueMessageProcessingResult Succeeded(MessageEnvelope envelope) =>
        new(QueueMessageProcessingStatus.Succeeded, envelope);

    public static QueueMessageProcessingResult Duplicate(MessageEnvelope envelope) =>
        new(QueueMessageProcessingStatus.Duplicate, envelope);

    public static QueueMessageProcessingResult Retryable(MessageEnvelope envelope) =>
        new(QueueMessageProcessingStatus.RetryableFailure, envelope);

    public static QueueMessageProcessingResult Permanent(MessageEnvelope? envelope = null) =>
        new(QueueMessageProcessingStatus.PermanentFailure, envelope);
}

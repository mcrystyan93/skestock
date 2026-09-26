using skestock.Domain.Queues;

namespace Worker.Queues;

/// <summary>
/// Describes what the polling module should do after the business module handled a message.
/// Keeping this result transport-neutral makes processing rules easy to test without Azure Queue
/// clients.
/// </summary>
/// <param name="Envelope">
/// The decoded envelope, or <see langword="null"/> when the raw message could not be decoded.
/// </param>
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

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
/// <param name="ErrorType">
/// Low-cardinality failure classification for metrics: the exception's full type name, or
/// <see cref="FailedResultErrorType"/> / <see cref="InvalidEnvelopeErrorType"/>.
/// </param>
public sealed record QueueMessageProcessingResult(
    QueueMessageProcessingStatus Status,
    MessageEnvelope? Envelope = null,
    string? ErrorType = null)
{
    public const string FailedResultErrorType = "failed_result";
    public const string InvalidEnvelopeErrorType = "invalid_envelope";

    public static QueueMessageProcessingResult Succeeded(MessageEnvelope envelope) =>
        new(QueueMessageProcessingStatus.Succeeded, envelope);

    public static QueueMessageProcessingResult Duplicate(MessageEnvelope envelope) =>
        new(QueueMessageProcessingStatus.Duplicate, envelope);

    public static QueueMessageProcessingResult Retryable(MessageEnvelope envelope, string errorType) =>
        new(QueueMessageProcessingStatus.RetryableFailure, envelope, errorType);

    public static QueueMessageProcessingResult Permanent(MessageEnvelope? envelope, string errorType) =>
        new(QueueMessageProcessingStatus.PermanentFailure, envelope, errorType);
}

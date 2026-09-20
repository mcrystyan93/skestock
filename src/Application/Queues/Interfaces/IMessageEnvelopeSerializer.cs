using skestock.Domain.Queues;

namespace skestock.Application.Queues.Interfaces;

/// <summary>
/// Defines the single wire-format contract shared by outbox publishing and Worker consumption.
/// Azure Queue messages are base64-encoded JSON envelopes; callers should not duplicate that
/// transport detail.
/// </summary>
public interface IMessageEnvelopeSerializer
{
    string Serialize(MessageEnvelope envelope);

    MessageEnvelope Deserialize(string encodedMessage);
}

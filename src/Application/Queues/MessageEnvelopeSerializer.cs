using System.Text;
using System.Text.Json;
using skestock.Application.Queues.Interfaces;
using skestock.Domain.Queues;

namespace skestock.Application.Queues;

/// <summary>
/// Encodes and decodes the queue wire format in one place so the publisher and Worker cannot
/// silently drift apart on JSON or base64 handling.
/// </summary>
public sealed class MessageEnvelopeSerializer : IMessageEnvelopeSerializer
{
    public string Serialize(MessageEnvelope envelope)
    {
        Guard.Against.Null(envelope);

        var json = JsonSerializer.Serialize(envelope);
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
    }

    public MessageEnvelope Deserialize(string encodedMessage)
    {
        Guard.Against.NullOrWhiteSpace(encodedMessage);

        var json = Encoding.UTF8.GetString(Convert.FromBase64String(encodedMessage));
        return JsonSerializer.Deserialize<MessageEnvelope>(json)
               ?? throw new InvalidOperationException("The queue message envelope was empty.");
    }
}

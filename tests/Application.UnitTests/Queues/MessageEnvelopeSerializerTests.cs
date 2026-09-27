using System.Text;
using NUnit.Framework;
using Shouldly;
using skestock.Application.Queues;
using skestock.Domain.Queues;

namespace skestock.Application.UnitTests.Queues;

public class MessageEnvelopeSerializerTests
{
    private readonly MessageEnvelopeSerializer _serializer = new();

    [Test]
    public void ShouldRoundTripTraceContext()
    {
        var envelope = new MessageEnvelope
        {
            MessageId = Guid.NewGuid(),
            Type = "Some.Type",
            Payload = "{}",
            UserId = Guid.NewGuid(),
            TraceParent = "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01",
            TraceState = "congo=t61rcWkgMzE"
        };

        var result = _serializer.Deserialize(_serializer.Serialize(envelope));

        result.TraceParent.ShouldBe(envelope.TraceParent);
        result.TraceState.ShouldBe(envelope.TraceState);
        result.MessageId.ShouldBe(envelope.MessageId);
    }

    [Test]
    public void ShouldDeserializeLegacyEnvelopeWithoutTraceContext()
    {
        var messageId = Guid.NewGuid();
        var legacyJson = $$"""{"MessageId":"{{messageId}}","Type":"Some.Type","Payload":"{}","UserId":null}""";
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(legacyJson));

        var result = _serializer.Deserialize(encoded);

        result.MessageId.ShouldBe(messageId);
        result.TraceParent.ShouldBeNull();
        result.TraceState.ShouldBeNull();
    }
}

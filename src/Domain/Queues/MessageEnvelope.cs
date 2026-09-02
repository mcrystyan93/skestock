namespace skestock.Domain.Queues;

public class MessageEnvelope
{
    public int MessageId { get; set; }
    public string Type { get; set; } = null!;
    public string Payload { get; set; } = null!;
}

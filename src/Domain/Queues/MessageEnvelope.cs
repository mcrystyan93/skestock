namespace skestock.Domain.Queues;

public class MessageEnvelope
{
    public Guid MessageId { get; set; }
    public string Type { get; set; } = null!;
    public string Payload { get; set; } = null!;
    public Guid? UserId { get; set; }
}

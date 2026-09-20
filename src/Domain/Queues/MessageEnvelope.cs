namespace skestock.Domain.Queues;

/// <summary>
/// Transport-neutral message contract stored in the outbox and carried through Azure Queue.
/// <see cref="Type"/> and <see cref="Payload"/> let the Worker restore the original Mediator
/// request, while <see cref="UserId"/> preserves the acting identity across the async seam.
/// </summary>
public class MessageEnvelope
{
    public Guid MessageId { get; set; }
    public string Type { get; set; } = null!;
    public string Payload { get; set; } = null!;
    public Guid? UserId { get; set; }
}

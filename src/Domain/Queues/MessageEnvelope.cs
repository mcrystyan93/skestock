namespace skestock.Domain.Queues;

/// <summary>
/// Transport-neutral message contract stored in the outbox and carried through Azure Queue.
/// <see cref="Type"/> and <see cref="Payload"/> let the Worker restore the original Mediator
/// request, while <see cref="UserId"/> preserves the acting identity across the async seam.
/// <see cref="TraceParent"/> and <see cref="TraceState"/> carry the W3C trace context so the
/// Worker continues the trace that produced the message; both are optional for older messages.
/// </summary>
public class MessageEnvelope
{
    public Guid MessageId { get; set; }
    public string Type { get; set; } = null!;
    public string Payload { get; set; } = null!;
    public Guid? UserId { get; set; }
    public string? TraceParent { get; set; }
    public string? TraceState { get; set; }
}

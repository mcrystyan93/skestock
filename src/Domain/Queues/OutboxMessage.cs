namespace skestock.Domain.Queues;

public class OutboxMessage
{
    public Guid Id { get; set; }
    public string Type { get; set; } = default!;
    public string Payload { get; set; } = default!;
    public string? QueueName { get; set; }          // optional: route to different queues
    public Guid? UserId { get; set; }                // identity id of the user who triggered the message
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ProcessedAtUtc { get; set; }
    public Guid? ClaimId { get; set; }               // publisher instance currently responsible for delivery
    public DateTimeOffset? ClaimedUntilUtc { get; set; }  // expired claims can be safely recovered
    public int RetryCount { get; set; }
    public string? Error { get; set; }
}

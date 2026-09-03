namespace skestock.Domain.Queues;

public class OutboxMessage
{
    public Guid Id { get; set; }
    public string Type { get; set; } = default!;
    public string Payload { get; set; } = default!;
    public string? QueueName { get; set; }          // optional: route to different queues
    public Guid? UserId { get; set; }                // identity id of the user who triggered the message
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ProcessedAtUtc { get; set; }
    public int RetryCount { get; set; }
    public string? Error { get; set; }
}

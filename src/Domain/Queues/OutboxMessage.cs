namespace skestock.Domain.Queues;

public class OutboxMessage
{
    public int Id { get; set; }
    public string Type { get; set; } = default!;
    public string Payload { get; set; } = default!;
    public string? QueueName { get; set; }          // optional: route to different queues
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ProcessedAtUtc { get; set; }
    public int RetryCount { get; set; }
    public string? Error { get; set; }
}

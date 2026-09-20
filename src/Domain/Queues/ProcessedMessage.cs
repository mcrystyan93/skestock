namespace skestock.Domain.Queues;

public class ProcessedMessage
{
    public Guid Id { get; set; }// reuse OutboxMessage.Id / envelope.MessageId as this PK
    public DateTimeOffset ProcessedAtUtc { get; set; }
    
}

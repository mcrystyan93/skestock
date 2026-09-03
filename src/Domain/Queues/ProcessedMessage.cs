namespace skestock.Domain.Queues;

public class ProcessedMessage
{
    public Guid Id { get; set; }// reuse OutboxMessage.Id / envelope.MessageId as this PK
    public DateTime ProcessedAtUtc { get; set; }
    
}

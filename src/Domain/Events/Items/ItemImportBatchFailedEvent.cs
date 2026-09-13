namespace skestock.Domain.Events.Items;

public class ItemImportBatchFailedEvent(Guid batchId) : BaseEvent
{
    public Guid BatchId { get; } = batchId;
}

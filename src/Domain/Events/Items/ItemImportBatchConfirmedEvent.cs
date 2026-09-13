namespace skestock.Domain.Events.Items;

public class ItemImportBatchConfirmedEvent(Guid batchId) : BaseEvent
{
    public Guid BatchId { get; } = batchId;
}

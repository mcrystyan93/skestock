using skestock.Domain.Entities;

namespace skestock.Domain.Events.Items;

public class ItemImportBatchCreatedEvent(ItemImportBatch batch) : BaseEvent
{
    public ItemImportBatch Batch { get; } = batch;
}

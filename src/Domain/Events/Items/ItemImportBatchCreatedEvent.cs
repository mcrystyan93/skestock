using skestock.Domain.Entities;
using skestock.Domain.Entities.Items;

namespace skestock.Domain.Events.Items;

public class ItemImportBatchCreatedEvent(ItemImportBatch batch) : BaseEvent
{
    public ItemImportBatch Batch { get; } = batch;
}

using skestock.Domain.Entities;

namespace skestock.Domain.Events.GoodsReceipt;

public class GoodsReceiptImportCompletedEvent(Guid importId): BaseEvent
{
    public Guid ImportId { get; } = importId;
}

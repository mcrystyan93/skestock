using skestock.Domain.Entities;

namespace skestock.Domain.Events.GoodsReceipt;

public class GoodsReceiptImportFailedEvent(Guid importId): BaseEvent
{
    public Guid ImportId { get; } = importId;
}

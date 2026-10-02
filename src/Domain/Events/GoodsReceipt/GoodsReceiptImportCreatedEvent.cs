using skestock.Domain.Entities;
using skestock.Domain.Entities.GoodsReceipts;

namespace skestock.Domain.Events.GoodsReceipt;

public class GoodsReceiptImportCreatedEvent(GoodsReceiptImport import) : BaseEvent
{
    public GoodsReceiptImport Import { get; } = import;
}

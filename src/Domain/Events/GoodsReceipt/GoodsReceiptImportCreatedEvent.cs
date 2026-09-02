using skestock.Domain.Entities;

namespace skestock.Domain.Events.GoodsReceipt;

public class GoodsReceiptImportCreatedEvent(GoodsReceiptImport import) : BaseEvent
{
    public GoodsReceiptImport Import { get; } = import;
}

namespace skestock.Domain.Events.GoodsReceipt;

public class GoodsReceiptImportConfirmedEvent(Guid importId) : BaseEvent
{
    public Guid ImportId { get; } = importId;
}

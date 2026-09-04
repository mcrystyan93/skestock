using skestock.Domain.Entities;

namespace skestock.Domain.Events.GoodsReceipt;

public class GoodsReceiptImportCompletedEvent(Guid importId, Guid uploadedByUserId): BaseEvent
{
    public Guid ImportId { get; } = importId;
    public Guid UploadedByUserId { get; } = uploadedByUserId;
}

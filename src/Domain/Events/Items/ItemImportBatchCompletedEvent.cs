namespace skestock.Domain.Events.Items;

public class ItemImportBatchCompletedEvent(Guid batchId, Guid uploadedByUserId) : BaseEvent
{
    public Guid BatchId { get; } = batchId;
    public Guid UploadedByUserId { get; } = uploadedByUserId;
}

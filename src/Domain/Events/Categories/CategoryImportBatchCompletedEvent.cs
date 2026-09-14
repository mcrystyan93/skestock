namespace skestock.Domain.Events.Categories;

public class CategoryImportBatchCompletedEvent(Guid batchId, Guid uploadedByUserId) : BaseEvent
{
    public Guid BatchId { get; } = batchId;
    public Guid UploadedByUserId { get; } = uploadedByUserId;
}

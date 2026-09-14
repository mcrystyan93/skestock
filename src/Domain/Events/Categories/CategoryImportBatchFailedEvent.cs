namespace skestock.Domain.Events.Categories;

public class CategoryImportBatchFailedEvent(Guid batchId) : BaseEvent
{
    public Guid BatchId { get; } = batchId;
}

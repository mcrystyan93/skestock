namespace skestock.Domain.Events.Categories;

public class CategoryImportBatchConfirmedEvent(Guid batchId) : BaseEvent
{
    public Guid BatchId { get; } = batchId;
}

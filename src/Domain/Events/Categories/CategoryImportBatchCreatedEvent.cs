using skestock.Domain.Entities;

namespace skestock.Domain.Events.Categories;

public class CategoryImportBatchCreatedEvent(CategoryImportBatch batch) : BaseEvent
{
    public CategoryImportBatch Batch { get; } = batch;
}

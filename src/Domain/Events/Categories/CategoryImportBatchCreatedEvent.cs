using skestock.Domain.Entities;
using skestock.Domain.Entities.Categories;

namespace skestock.Domain.Events.Categories;

public class CategoryImportBatchCreatedEvent(CategoryImportBatch batch) : BaseEvent
{
    public CategoryImportBatch Batch { get; } = batch;
}

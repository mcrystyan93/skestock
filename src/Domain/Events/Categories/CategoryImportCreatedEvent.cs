using skestock.Domain.Entities;

namespace skestock.Domain.Events.Categories;

public class CategoryImportCreatedEvent(CategoryImport import) : BaseEvent
{
    public CategoryImport Import { get; } = import;
}

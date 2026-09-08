namespace skestock.Domain.Events.Categories;

public class CategoryImportFailedEvent(Guid importId) : BaseEvent
{
    public Guid ImportId { get; } = importId;
}

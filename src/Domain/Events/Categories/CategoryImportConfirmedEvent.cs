namespace skestock.Domain.Events.Categories;

public class CategoryImportConfirmedEvent(Guid importId) : BaseEvent
{
    public Guid ImportId { get; } = importId;
}

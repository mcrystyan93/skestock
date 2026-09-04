using skestock.Domain.Entities;

namespace skestock.Domain.Events.Categories;

public class CategoryUpdatedEvent(Category category) : BaseEvent
{
    public Category Category { get; } = category;
}

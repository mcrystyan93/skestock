using skestock.Domain.Entities;
using skestock.Domain.Entities.Categories;

namespace skestock.Domain.Events.Categories;

public class CategoryUpdatedEvent(Category category) : BaseEvent
{
    public Category Category { get; } = category;
}

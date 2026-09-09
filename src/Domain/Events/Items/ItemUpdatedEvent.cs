using skestock.Domain.Entities;

namespace skestock.Domain.Events.Items;

public class ItemUpdatedEvent(Item item) : BaseEvent
{
    public Item Item { get; } = item;
}

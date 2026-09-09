using skestock.Domain.Entities;

namespace skestock.Domain.Events.Items;

public class ItemDisabledEvent(Item item) : BaseEvent
{
    public Item Item { get; } = item;
}

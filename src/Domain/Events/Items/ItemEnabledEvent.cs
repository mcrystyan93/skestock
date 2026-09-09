using skestock.Domain.Entities;

namespace skestock.Domain.Events.Items;

public class ItemEnabledEvent(Item item) : BaseEvent
{
    public Item Item { get; } = item;
}

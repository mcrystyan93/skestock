using skestock.Domain.Entities;
using skestock.Domain.Entities.Items;

namespace skestock.Domain.Events.Items;

public class ItemDisabledEvent(Item item) : BaseEvent
{
    public Item Item { get; } = item;
}

using skestock.Domain.Entities;

namespace skestock.Domain.Events.Items;

public class ItemImportCreatedEvent(ItemImport import) : BaseEvent
{
    public ItemImport Import { get; } = import;
}

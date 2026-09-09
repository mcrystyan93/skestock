namespace skestock.Domain.Events.Items;

public class ItemImportFailedEvent(Guid importId) : BaseEvent
{
    public Guid ImportId { get; } = importId;
}

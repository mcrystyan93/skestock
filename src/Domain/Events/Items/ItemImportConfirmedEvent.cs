namespace skestock.Domain.Events.Items;

public class ItemImportConfirmedEvent(Guid importId) : BaseEvent
{
    public Guid ImportId { get; } = importId;
}

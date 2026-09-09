namespace skestock.Domain.Events.Items;

public class ItemImportCompletedEvent(Guid importId, Guid uploadedByUserId) : BaseEvent
{
    public Guid ImportId { get; } = importId;
    public Guid UploadedByUserId { get; } = uploadedByUserId;
}

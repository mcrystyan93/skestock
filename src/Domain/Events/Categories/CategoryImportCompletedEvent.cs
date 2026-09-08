namespace skestock.Domain.Events.Categories;

public class CategoryImportCompletedEvent(Guid importId, Guid uploadedByUserId) : BaseEvent
{
    public Guid ImportId { get; } = importId;
    public Guid UploadedByUserId { get; } = uploadedByUserId;
}

namespace skestock.Domain.Events.StockBatches;

public sealed class StockBatchCreatedEvent(Guid classId, Guid locationId) : BaseEvent
{
    public Guid ClassId { get; } = classId;
    public Guid LocationId { get; } = locationId;
}

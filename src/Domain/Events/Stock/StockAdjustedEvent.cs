namespace skestock.Domain.Events.Stock;

public sealed class StockAdjustedEvent(Guid classId, Guid locationId) : BaseEvent
{
    public Guid ClassId { get; } = classId;
    public Guid LocationId { get; } = locationId;
}

namespace skestock.Domain.Events.Stock;

public sealed class StockMovedEvent(
    Guid classId,
    Guid itemId,
    Guid sourceLocationId,
    Guid destinationLocationId,
    int quantity) : BaseEvent
{
    public Guid ClassId { get; } = classId;
    public Guid ItemId { get; } = itemId;
    public Guid SourceLocationId { get; } = sourceLocationId;
    public Guid DestinationLocationId { get; } = destinationLocationId;
    public int Quantity { get; } = quantity;
}

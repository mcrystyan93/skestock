namespace skestock.Domain.Events.Stock;

public sealed class ClassItemStockVisibilityChangedEvent(
    Guid classId,
    Guid itemId,
    Guid locationId,
    bool hideWhenZeroStock) : BaseEvent
{
    public Guid ClassId { get; } = classId;
    public Guid ItemId { get; } = itemId;
    public Guid LocationId { get; } = locationId;
    public bool HideWhenZeroStock { get; } = hideWhenZeroStock;
}

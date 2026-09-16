namespace skestock.Domain.Events.Stock;

public sealed class ClassItemStockVisibilityChangedEvent(
    Guid classId,
    Guid itemId,
    bool hideWhenZeroStock) : BaseEvent
{
    public Guid ClassId { get; } = classId;
    public Guid ItemId { get; } = itemId;
    public bool HideWhenZeroStock { get; } = hideWhenZeroStock;
}

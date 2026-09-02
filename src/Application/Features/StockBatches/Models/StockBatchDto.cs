namespace skestock.Application.Features.StockBatches.Models;

/// <summary>
/// Row shape for the paginated stock-batch list. Flattens the item/location names in so
/// callers don't have to make follow-up requests to resolve them.
/// </summary>
public record StockBatchListItemDto
{
    public Guid Id { get; init; }
    public Guid ItemId { get; init; }
    public string ItemName { get; init; } = string.Empty;
    public Guid LocationId { get; init; }
    public string LocationName { get; init; } = string.Empty;
    public Guid? GoodsReceiptId { get; init; }
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal LineTotal { get; init; }
    public DateOnly? ExpiryDate { get; init; }
    public DateOnly ReceivedDate { get; init; }
    public DateTimeOffset CreatedDate { get; init; }
}

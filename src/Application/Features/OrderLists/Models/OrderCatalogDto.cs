namespace skestock.Application.Features.OrderLists.Models;

// One orderable item. Empty Locations means the item has no class stock and no default location.
public record OrderCatalogItemDto
{
    public Guid ItemId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Sku { get; init; }
    public Guid? ImageFileId { get; init; }
    public string Unit { get; init; } = string.Empty;
    public int? LastQuantity { get; init; }
    // Receipts in the last year (0 when never ordered): the frequency the client sorts by.
    public int PurchaseCount { get; init; }
    // Date of the last receipt in the last year; null when the item was never ordered.
    public DateTimeOffset? LastPurchasedAt { get; init; }
    public decimal? AverageIntervalDays { get; init; }
    public bool IsFrequent { get; init; }
    public List<OrderCatalogLocationDto> Locations { get; init; } = [];
}

public record OrderCatalogLocationDto
{
    public Guid LocationId { get; init; }
    public string LocationName { get; init; } = string.Empty;
    // Current stock at this location (0 for the default-location fallback, which has no class stock).
    public int Quantity { get; init; }
    public bool IsLowStock { get; init; }
}

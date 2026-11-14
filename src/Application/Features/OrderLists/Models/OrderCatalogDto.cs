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
    public decimal? AverageIntervalDays { get; init; }
    public bool IsFrequent { get; init; }
    public List<OrderCatalogLocationDto> Locations { get; init; } = [];
}

public record OrderCatalogLocationDto
{
    public Guid LocationId { get; init; }
    public string LocationName { get; init; } = string.Empty;
    public bool IsLowStock { get; init; }
}

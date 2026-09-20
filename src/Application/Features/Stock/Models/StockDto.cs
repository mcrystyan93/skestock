using skestock.Application.Features.Categories.Models;

namespace skestock.Application.Features.Stock.Models;

public record StockReportDto
{
    public List<StockItemDto> Items { get; init; } = [];
    public bool HasExpiredItems { get; init; }
}

public record StockItemDto
{
    public Guid ItemId { get; init; }
    public string ItemName { get; init; } = string.Empty;
    public string? Sku { get; init; }
    public Guid CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public CategoryIconDto? CategoryIcon { get; init; }
    public Guid LocationId { get; init; }
    public string LocationName { get; init; } = string.Empty;
    public DateTimeOffset? LastUpdatedAt { get; init; }
    public string Unit { get; init; } = string.Empty;
    public bool IsPerishable { get; init; }
    public bool IsExpired { get; init; }
    public int ExpiredQuantity { get; init; }
    public int Quantity { get; init; }
    public bool IsLowStock { get; init; }
    public bool HideWhenZeroStock { get; init; }
}

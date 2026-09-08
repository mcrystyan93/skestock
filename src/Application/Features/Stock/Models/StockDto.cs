using skestock.Application.Features.Categories.Models;

namespace skestock.Application.Features.Stock.Models;

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
    public string Unit { get; init; } = string.Empty;
    public bool IsPerishable { get; init; }
    public int Quantity { get; init; }
    public bool IsLowStock { get; init; }
}

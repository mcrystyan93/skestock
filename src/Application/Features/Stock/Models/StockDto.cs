namespace skestock.Application.Features.Stock.Models;

public record StockItemDto
{
    public int ItemId { get; init; }
    public string ItemName { get; init; } = string.Empty;
    public int CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public int LocationId { get; init; }
    public string LocationName { get; init; } = string.Empty;
    public string Unit { get; init; } = string.Empty;
    public bool IsPerishable { get; init; }
    public int Quantity { get; init; }
    public bool IsLowStock { get; init; }
}

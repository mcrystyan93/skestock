namespace skestock.Application.Features.Stock.Models;

public record StockItemDto
{
    public Guid ItemId { get; init; }
    public string ItemName { get; init; } = string.Empty;
    public Guid CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public Guid LocationId { get; init; }
    public string LocationName { get; init; } = string.Empty;
    public string Unit { get; init; } = string.Empty;
    public bool IsPerishable { get; init; }
    public int Quantity { get; init; }
    public bool IsLowStock { get; init; }
}

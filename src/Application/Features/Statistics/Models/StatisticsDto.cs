namespace skestock.Application.Features.Statistics.Models;

public record CategoryStockSummaryDto
{
    public Guid CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public int ItemCount { get; init; }
}

public record ClassStockByCategoryDto
{
    public List<CategoryStockSummaryDto> Categories { get; init; } = [];
    public int TotalQuantity { get; init; }
    public int TotalItemCount { get; init; }
    public int TotalCategoryCount { get; init; }
}

public record GoodsReceiptCostPointDto
{
    public Guid Id { get; init; }
    public DateTime ReceivedAt { get; init; }
    public decimal TotalAmount { get; init; }
    public string? SupplierReference { get; init; }
}

public record ClassGoodsReceiptCostsDto
{
    public List<GoodsReceiptCostPointDto> Points { get; init; } = [];
    public int ReceiptCount { get; init; }
    public decimal TotalAmount { get; init; }
    public decimal AverageAmount { get; init; }
}

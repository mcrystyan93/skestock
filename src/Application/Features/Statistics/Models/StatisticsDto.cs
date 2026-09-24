namespace skestock.Application.Features.Statistics.Models;

public record ClassStockByCategorySeriesDto
{
    public string Name { get; init; } = string.Empty;
    public List<int> Data { get; init; } = [];
}

public record ClassStockByCategoryDto
{
    public List<string> Labels { get; init; } = [];
    public List<ClassStockByCategorySeriesDto> Series { get; init; } = [];
}

public record GoodsReceiptCostPointDto
{
    public Guid Id { get; init; }
    public DateTimeOffset ReceivedAt { get; init; }
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

public record ClassItemStockEvolutionPointDto
{
    public DateOnly Date { get; init; }
    public int CumulativeQuantity { get; init; }
}

public record ClassItemStockEvolutionDto
{
    public Guid ItemId { get; init; }
    public string ItemName { get; init; } = string.Empty;
    public string? Sku { get; init; }
    public string Unit { get; init; } = string.Empty;
    public List<ClassItemStockEvolutionPointDto> Points { get; init; } = [];
}

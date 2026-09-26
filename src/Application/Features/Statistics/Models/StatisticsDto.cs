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

public record DailyConsumptionAverageDto
{
    public DateOnly FromDate { get; init; }
    public DateOnly ToDate { get; init; }
    public int Days { get; init; }
    public int TotalQuantity { get; init; }
    public decimal TotalValue { get; init; }
    public decimal AverageQuantity { get; init; }
    public decimal AverageValue { get; init; }
}

public record DailyConsumptionAveragesDto
{
    public DailyConsumptionAverageDto Last7Days { get; init; } = new();
    public DailyConsumptionAverageDto Last30Days { get; init; } = new();
}

public record DailyConsumptionPointDto
{
    public DateOnly Date { get; init; }
    public int Quantity { get; init; }
    public decimal Value { get; init; }
}

public record ClassDailyConsumptionDto
{
    public Guid ClassId { get; init; }
    public DateOnly? FromDate { get; init; }
    public DateOnly? ToDate { get; init; }
    public List<DailyConsumptionPointDto> Points { get; init; } = [];
    public int TotalQuantity { get; init; }
    public decimal TotalValue { get; init; }
    public decimal AverageQuantity { get; init; }
    public decimal AverageValue { get; init; }
}

public record PurchaseStatisticDto
{
    public Guid ItemId { get; init; }
    public string ItemName { get; init; } = string.Empty;
    public string? Sku { get; init; }
    public string Unit { get; init; } = string.Empty;
    public string CategoryName { get; init; } = string.Empty;
    public int TotalQuantity { get; init; }
    public decimal TotalValue { get; init; }
    public int PurchaseCount { get; init; }
    public decimal AverageQuantity { get; init; }
    public decimal AverageUnitPrice { get; init; }
    public DateTimeOffset LastPurchasedAt { get; init; }
}

public record TopPurchasesDto
{
    public DateTimeOffset? ComputedAt { get; init; }
    public List<PurchaseStatisticDto> ByQuantity { get; init; } = [];
    public List<PurchaseStatisticDto> ByValue { get; init; } = [];
    public List<PurchaseStatisticDto> ByFrequency { get; init; } = [];
}

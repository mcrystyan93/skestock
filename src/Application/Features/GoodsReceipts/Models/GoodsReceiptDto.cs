namespace skestock.Application.Features.GoodsReceipts.Models;

public record GoodsReceiptLineDto
{
    public int StockBatchId { get; init; }
    public int ItemId { get; init; }
    public string ItemName { get; init; } = string.Empty;
    public int LocationId { get; init; }
    public string LocationName { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public DateOnly? ExpiryDate { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal LineTotal { get; init; }
}

public record GoodsReceiptDto
{
    public int Id { get; init; }
    public int ClassId { get; init; }
    public string ClassName { get; init; } = string.Empty;
    public DateTime ReceivedAt { get; init; }
    public string? SupplierReference { get; init; }
    public string Note { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public List<GoodsReceiptLineDto> Lines { get; init; } = [];
    public string? CreatedByName { get; init; }
    public string? LastModifiedByName { get; init; }
    public DateTimeOffset CreatedDate { get; init; }
    public DateTimeOffset LastModifiedDate { get; init; }
}

/// <summary>
/// Lightweight row shape for the paginated goods-receipt list - a line count/quantity summary
/// instead of the full line detail (see <see cref="GoodsReceiptDto"/>), so listing many receipts
/// doesn't have to join and materialize every batch of every receipt.
/// </summary>
public record GoodsReceiptListItemDto
{
    public int Id { get; init; }
    public int ClassId { get; init; }
    public string ClassName { get; init; } = string.Empty;
    public DateTime ReceivedAt { get; init; }
    public string? SupplierReference { get; init; }
    public string Note { get; init; } = string.Empty;
    public int LineCount { get; init; }
    public int TotalQuantity { get; init; }
    public decimal TotalAmount { get; init; }
    public string? CreatedByName { get; init; }
    public DateTimeOffset CreatedDate { get; init; }
}

namespace skestock.Application.Features.GoodsReceipts.Models;

public record GoodsReceiptImportLineDto
{
    public Guid Id { get; init; }
    public Guid ImportId { get; init; }
    public string RawItemText { get; init; } = string.Empty;
    public Guid? MatchedItemId { get; init; }
    public Guid? LocationId { get; init; }
    public string? LocationName { get; init; }
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public DateOnly? ExpiryDate { get; init; }
    public decimal LineTotal { get; init; }
}

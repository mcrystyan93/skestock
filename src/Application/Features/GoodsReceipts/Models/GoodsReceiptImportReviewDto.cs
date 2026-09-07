using skestock.Domain.Enums;

namespace skestock.Application.Features.GoodsReceipts.Models;

/// <summary>
/// The AI extraction for a single import, joined against the item catalog so each extracted line
/// carries its best-guess catalog match (by <see cref="Domain.Entities.Item.Sku"/>) - or null when
/// no item matches, meaning the line will create a new item on confirm. Drives the review screen.
/// </summary>
public record GoodsReceiptImportReviewDto
{
    public Guid Id { get; init; }
    public GoodsReceiptImportStatus Status { get; init; }
    public Guid ClassId { get; init; }
    public string ClassName { get; init; } = string.Empty;
    public string? SupplierReference { get; init; }
    public DateOnly? ReceivedAt { get; init; }
    public List<GoodsReceiptImportReviewLineDto> Lines { get; init; } = [];
}

public record GoodsReceiptImportReviewLineDto
{
    /// <summary>Raw text the AI read off the document, for the reviewer to sanity-check against.</summary>
    public string? RawItemText { get; init; }
    public string? ProductCode { get; init; }
    public string? Name { get; init; }
    public string? Unit { get; init; }
    public string? Category { get; init; }
    public decimal Quantity { get; init; }
    public decimal? UnitPrice { get; init; }
    public bool IsPerishable { get; init; }

    /// <summary>The catalog item this line matched by SKU, or null when nothing matched.</summary>
    public GoodsReceiptImportReviewMatchDto? MatchedItem { get; init; }
}

public record GoodsReceiptImportReviewMatchDto
{
    public Guid Id { get; init; }
    public string? Sku { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Unit { get; init; } = string.Empty;
    public bool IsPerishable { get; init; }
    public int? ShelfLifeDays { get; set; }
    public Guid CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
}

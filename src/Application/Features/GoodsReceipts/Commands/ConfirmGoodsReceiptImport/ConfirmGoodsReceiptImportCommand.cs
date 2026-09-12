using skestock.Application.Common.Caching;
using skestock.Application.Common.Security;
using skestock.Application.Features.GoodsReceipts.Models;
using CategoryCacheConstants = skestock.Application.Features.Categories.CacheConstants;
using ItemCacheConstants = skestock.Application.Features.Items.CacheConstants;
using StockCacheConstants = skestock.Application.Features.Stock.CacheConstants;
using StatisticsCacheConstants = skestock.Application.Features.Statistics.CacheConstants;

namespace skestock.Application.Features.GoodsReceipts.Commands.ConfirmGoodsReceiptImport;

/// <summary>
/// One reviewed import line. Either references an existing catalog item (<see cref="ItemId"/> set),
/// or, when <see cref="ItemId"/> is null, carries the fields needed to create a new item on confirm.
/// Splitting a source line across locations produces several of these lines.
/// </summary>
public class ConfirmGoodsReceiptImportLine
{
    // Existing item path.
    public Guid? ItemId { get; init; }

    // New item path (used only when ItemId is null).
    public string? Name { get; init; }
    public string? Sku { get; init; }
    public string? Unit { get; init; }
    public string? CategoryName { get; init; }
    public bool IsPerishable { get; init; }

    public Guid LocationId { get; init; }
    public int Quantity { get; init; }
    public DateOnly? ExpiryDate { get; init; }
    public decimal UnitPrice { get; init; }

    /// <summary>
    /// 0-based index of the extraction line this confirm line came from. Multiple confirm lines can
    /// share one source index (a split); the server checks their quantities sum back to the original.
    /// </summary>
    public int SourceLineIndex { get; init; }
}

// Requires an authenticated user for the same reason CreateGoodsReceiptCommand does:
// StockTransaction.UserId (who physically received the goods) is required.
[Authorize]
public class ConfirmGoodsReceiptImportCommand : IRequest<Result<GoodsReceiptDto>>, ICacheInvalidation
{
    public Guid ImportId { get; init; }
    public string? SupplierReference { get; init; }
    public string Note { get; init; } = string.Empty;
    public DateOnly ReceivedDate { get; set; }
    public List<ConfirmGoodsReceiptImportLine> Lines { get; init; } = [];

    // ClassId is resolved from the import (locked in at upload time), not supplied by the caller,
    // so it isn't a command field. Invalidate the goods-receipt + import lists, the item/category
    // lists (new items/categories may be created), and the stock reports for every (class,
    // location) pair this receipt's lines touch plus the class-wide report.
    public Guid ClassId { get; set; }

    public IReadOnlyCollection<string> Tags =>
        [
            CacheConstants.GoodsReceiptListTag,
            CacheConstants.GoodsReceiptImportListTag,
            StatisticsCacheConstants.BuildClassGoodsReceiptCostTag(ClassId),
            ItemCacheConstants.ItemListTag,
            CategoryCacheConstants.CategoryListTag,
            StockCacheConstants.BuildClassTag(ClassId),
            ..Lines.Select(l => l.LocationId).Distinct().Select(locationId => StockCacheConstants.BuildTag(ClassId, locationId))
        ];
}

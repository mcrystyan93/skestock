using skestock.Application.Common.Caching;
using skestock.Application.Common.Security;
using skestock.Application.Features.GoodsReceipts.Models;
using StockCacheConstants = skestock.Application.Features.Stock.CacheConstants;

namespace skestock.Application.Features.GoodsReceipts.Commands.CreateGoodsReceipt;

public class CreateGoodsReceiptLine
{
    public int ItemId { get; init; }
    public int LocationId { get; init; }
    public int Quantity { get; init; }
    public DateOnly? ExpiryDate { get; init; }
    public decimal UnitPrice { get; init; }
}

// Requires an authenticated user: StockTransaction.UserId (who physically received the goods)
// is a required field distinct from the CreatedBy audit column, so an anonymous caller can't be
// allowed through to the handler.
[Authorize]
public class CreateGoodsReceiptCommand : IRequest<Result<GoodsReceiptDto>>, ICacheInvalidation
{
    public int ClassId { get; init; }
    public string? SupplierReference { get; init; }
    public string Note { get; init; } = string.Empty;
    public List<CreateGoodsReceiptLine> Lines { get; init; } = [];

    // Invalidate every cached GetAllGoodsReceipts page/filter/sort combination - a new receipt
    // can affect any of them (default sort, date-range filters, etc.) - plus the current-stock
    // report for every (class, location) pair this receipt's lines touch, since each line adds a
    // new StockBatch that changes that report's sum. Also invalidate the class-wide (all
    // locations) stock report, since it aggregates across every location too.
    public IReadOnlyCollection<string> Tags =>
        [
            CacheConstants.GoodsReceiptListTag,
            StockCacheConstants.BuildClassTag(ClassId),
            ..Lines.Select(l => l.LocationId).Distinct().Select(locationId => StockCacheConstants.BuildTag(ClassId, locationId))
        ];
}

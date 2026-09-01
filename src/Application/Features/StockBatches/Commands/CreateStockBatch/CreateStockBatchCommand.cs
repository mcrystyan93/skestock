using skestock.Application.Common.Caching;
using skestock.Application.Common.Security;
using skestock.Application.Features.StockBatches.Models;
using StockCacheConstants = skestock.Application.Features.Stock.CacheConstants;

namespace skestock.Application.Features.StockBatches.Commands.CreateStockBatch;

// Requires an authenticated user: StockTransaction.UserId (who logged this stock) is a required
// field distinct from the CreatedBy audit column, same rationale as
// CreateGoodsReceiptCommand/AdjustStockCommand.
[Authorize]
public class CreateStockBatchCommand : IRequest<Result<StockBatchListItemDto>>, ICacheInvalidation
{
    public int ItemId { get; init; }
    public int LocationId { get; init; }
    public int ReceivedClassId { get; init; }
    public int Quantity { get; init; }
    public DateOnly? ExpiryDate { get; init; }
    public DateOnly ReceivedDate { get; init; }
    public decimal UnitPrice { get; init; }

    // Invalidate every cached GetAllStockBatches page/filter/sort combination (this batch can
    // appear in any of them), plus the current-stock report for this exact (class, location)
    // pair and the class-wide (all locations) report - mirrors CreateGoodsReceiptCommand's tags,
    // since this batch changes those reports' sums the same way a goods-receipt line does.
    public IReadOnlyCollection<string> Tags =>
        [
            CacheConstants.StockBatchListTag,
            StockCacheConstants.BuildTag(ReceivedClassId, LocationId),
            StockCacheConstants.BuildClassTag(ReceivedClassId)
        ];
}

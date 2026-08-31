using skestock.Application.Common.Caching;
using skestock.Application.Common.Security;
using skestock.Application.Features.Stock.Models;
using skestock.Domain.Enums;

namespace skestock.Application.Features.Stock.Commands.AdjustStock;

// Requires an authenticated user: StockTransaction.UserId (who performed the recount) is a
// required field distinct from the CreatedBy audit column, same rationale as
// CreateGoodsReceiptCommand.
[Authorize]
public class AdjustStockCommand : IRequest<Result<StockItemDto>>, ICacheInvalidation
{
    public int ClassId { get; init; }
    public int ItemId { get; init; }
    public int LocationId { get; init; }

    // The quantity staff physically counted. The handler diffs this against the current total
    // (sum of StockBatch.Quantity for this Item+Location+Class) to compute the adjustment delta.
    public int ActualQuantity { get; init; }
    public AdjustmentReason Reason { get; init; }

    // Invalidates the current-stock report for this exact (class, location) pair, plus the
    // class-wide (all locations) report, mirroring CreateGoodsReceiptCommand's tags - an
    // adjustment changes StockBatch quantities the same way a new receipt does.
    public IReadOnlyCollection<string> Tags =>
        [
            CacheConstants.BuildTag(ClassId, LocationId),
            CacheConstants.BuildClassTag(ClassId)
        ];
}

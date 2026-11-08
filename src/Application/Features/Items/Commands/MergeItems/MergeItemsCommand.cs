using skestock.Application.Common.Caching;
using skestock.Application.Common.Security;
using skestock.Domain.Constants;
using StockCacheConstants = skestock.Application.Features.Stock.CacheConstants;

namespace skestock.Application.Features.Items.Commands.MergeItems;

/// <summary>Re-points everything referencing the secondary items to the primary and disables the secondaries. <paramref name="ItemIds"/> includes the primary.</summary>
[Authorize(Roles = Roles.Administrator)]
public sealed record MergeItemsCommand(Guid PrimaryItemId, IReadOnlyCollection<Guid> ItemIds)
    : IRequest<Result>, ICacheInvalidation
{
    public IReadOnlyCollection<string> Tags =>
    [
        CacheConstants.ItemListTag,
        .. ItemIds.Select(CacheConstants.ItemTag),
        StockCacheConstants.BuildCoarseTag(),
        StockBatches.CacheConstants.StockBatchListTag,
        OrderLists.CacheConstants.OrderListListTag,
        SupplyLists.CacheConstants.SupplyListListTag,
        GoodsReceipts.CacheConstants.GoodsReceiptImportListTag,
        Statistics.CacheConstants.PurchaseStatisticsTag,
        Statistics.CacheConstants.DailyConsumptionTag,
        Categories.CacheConstants.CategoryListTag
    ];
}

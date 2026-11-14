using skestock.Application.Common.Caching;
using skestock.Application.Common.Security;
using skestock.Application.Features.OrderLists.Models;
using skestock.Application.Features.Items;
using StockCache = skestock.Application.Features.Stock.CacheConstants;
using StatisticsCache = skestock.Application.Features.Statistics.CacheConstants;

namespace skestock.Application.Features.OrderLists.Queries.GetOrderCatalog;

// Every active item with its class locations, last ordered quantity and purchase cadence, for the order wizard.
[Authorize]
public class GetOrderCatalogQuery : IRequest<Result<List<OrderCatalogItemDto>>>, ICacheableQuery
{
    public Guid ClassId { get; init; }

    public IReadOnlyCollection<string> Tags =>
    [
        Items.CacheConstants.ItemListTag,
        StockCache.BuildClassTag(ClassId),
        StockCache.BuildCoarseTag(),
        StatisticsCache.PurchaseStatisticsTag
    ];

    public bool BypassCache => false;

    public TimeSpan? SlidingExpiration =>
        SlidingExpirationHelper.GetRandomizedSlidingExpiration(TimeSpan.FromMinutes(5), 30);

    public string BuildCacheKey() => $"{CacheConstants.OrderList}:catalog:class:{ClassId}";
}

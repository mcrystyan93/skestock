using skestock.Application.Common.Caching;
using skestock.Application.Common.Security;
using skestock.Application.Features.Statistics.Models;
using skestock.Domain.Enums;

namespace skestock.Application.Features.Statistics.Queries.GetTopPurchases;

// Items ranked by purchased quantity, purchase value and purchase frequency within a scope,
// read from the materialized ItemPurchaseStatistics rows.
[Authorize]
public class GetTopPurchasesQuery : IRequest<Result<TopPurchasesDto>>, ICacheableQuery
{
    public const int DefaultTop = 10;
    public const int MaxTop = 50;

    public PurchaseStatisticsScope Scope { get; init; } = PurchaseStatisticsScope.Last365Days;

    // Required when Scope == Class; ignored otherwise.
    public Guid? ClassId { get; init; }

    public Guid? CategoryId { get; init; }

    public int Top { get; init; } = DefaultTop;

    public IReadOnlyCollection<string> Tags => [CacheConstants.PurchaseStatisticsTag];

    public bool BypassCache => false;

    public TimeSpan? SlidingExpiration =>
        SlidingExpirationHelper.GetRandomizedSlidingExpiration(TimeSpan.FromMinutes(10), 30);

    public string BuildCacheKey() =>
        $"{CacheConstants.PurchaseStatisticsTag}:top:{Scope}:" +
        $"class:{(Scope == PurchaseStatisticsScope.Class ? ClassId?.ToString() : null) ?? "all"}:" +
        $"category:{CategoryId?.ToString() ?? "all"}:top:{Top}";
}

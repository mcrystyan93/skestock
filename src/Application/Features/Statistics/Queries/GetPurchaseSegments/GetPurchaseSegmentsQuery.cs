using skestock.Application.Common.Caching;
using skestock.Application.Common.Security;
using skestock.Application.Features.Statistics.Models;

namespace skestock.Application.Features.Statistics.Queries.GetPurchaseSegments;

// Active items grouped by how often they are bought (cadence) or bought about once per class,
// read from the materialized ItemPurchaseStatistics rows.
[Authorize]
public class GetPurchaseSegmentsQuery : IRequest<Result<PurchaseSegmentsDto>>, ICacheableQuery
{
    public IReadOnlyCollection<string> Tags => [CacheConstants.PurchaseStatisticsTag];

    public bool BypassCache => false;

    public TimeSpan? SlidingExpiration =>
        SlidingExpirationHelper.GetRandomizedSlidingExpiration(TimeSpan.FromMinutes(10), 30);

    public string BuildCacheKey() => $"{CacheConstants.PurchaseStatisticsTag}:segments";
}

using System.Security.Cryptography;
using System.Text;
using skestock.Application.Common.Caching;
using skestock.Application.Common.Security;
using skestock.Application.Features.Statistics.Models;

namespace skestock.Application.Features.Statistics.Queries.GetItemsPurchaseHistory;

// Last-365-days purchase history for a set of items, used as a hint while preparing an order list.
// Items that were not purchased in the window are omitted.
[Authorize]
public class GetItemsPurchaseHistoryQuery : IRequest<Result<List<PurchaseStatisticDto>>>, ICacheableQuery
{
    public const int MaxItems = 200;

    public IReadOnlyCollection<Guid> ItemIds { get; init; } = [];

    public IReadOnlyCollection<string> Tags => [CacheConstants.PurchaseStatisticsTag];

    public bool BypassCache => false;

    public TimeSpan? SlidingExpiration =>
        SlidingExpirationHelper.GetRandomizedSlidingExpiration(TimeSpan.FromMinutes(10), 30);

    public string BuildCacheKey()
    {
        var normalized = string.Join(',', ItemIds.Distinct().Order());
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)));
        return $"{CacheConstants.PurchaseStatisticsTag}:items-history:{hash}";
    }
}

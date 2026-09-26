using skestock.Application.Common.Caching;
using skestock.Application.Common.Security;
using skestock.Application.Features.Statistics.Models;

namespace skestock.Application.Features.Statistics.Queries.GetDailyConsumptionAverages;

// Average daily consumption over the last 7 and 30 complete local days (ending yesterday),
// optionally narrowed by item, location and item category.
[Authorize]
public class GetDailyConsumptionAveragesQuery : IRequest<Result<DailyConsumptionAveragesDto>>, ICacheableQuery
{
    public Guid? ItemId { get; init; }
    public Guid? LocationId { get; init; }
    public Guid? CategoryId { get; init; }

    public IReadOnlyCollection<string> Tags => [CacheConstants.DailyConsumptionTag];

    public bool BypassCache => false;

    public TimeSpan? SlidingExpiration =>
        SlidingExpirationHelper.GetRandomizedSlidingExpiration(TimeSpan.FromMinutes(10), 30);

    public string BuildCacheKey() =>
        $"{CacheConstants.DailyConsumptionTag}:averages:{DailyConsumptionCalendar.CurrentDateKey()}:" +
        $"item:{ItemId?.ToString() ?? "all"}:location:{LocationId?.ToString() ?? "all"}:" +
        $"category:{CategoryId?.ToString() ?? "all"}";
}

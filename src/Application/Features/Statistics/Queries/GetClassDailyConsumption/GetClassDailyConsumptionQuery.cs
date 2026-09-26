using skestock.Application.Common.Caching;
using skestock.Application.Common.Security;
using skestock.Application.Features.Statistics.Models;

namespace skestock.Application.Features.Statistics.Queries.GetClassDailyConsumption;

// Zero-filled daily consumption series for a class, from the local date of its first stock
// transaction (which may precede the class start date) until today or the class end.
[Authorize]
public class GetClassDailyConsumptionQuery : IRequest<Result<ClassDailyConsumptionDto>>, ICacheableQuery
{
    public Guid ClassId { get; init; }

    public IReadOnlyCollection<string> Tags => [CacheConstants.DailyConsumptionTag];

    public bool BypassCache => false;

    public TimeSpan? SlidingExpiration =>
        SlidingExpirationHelper.GetRandomizedSlidingExpiration(TimeSpan.FromMinutes(10), 30);

    public string BuildCacheKey() =>
        $"{CacheConstants.DailyConsumptionTag}:class:{ClassId}:{DailyConsumptionCalendar.CurrentDateKey()}";
}

using skestock.Application.Common.Caching;
using skestock.Application.Features.Stock.Models;

namespace skestock.Application.Features.Stock.Queries.GetClassLocationStock;

public class GetClassLocationStockQuery : IRequest<Result<List<StockItemDto>>>, ICacheableQuery<List<StockItemDto>>
{
    public int ClassId { get; init; }
    public int LocationId { get; init; }

    public IReadOnlyCollection<string> Tags => [CacheConstants.BuildTag(ClassId, LocationId)];
    public bool BypassCache => false;
    public TimeSpan? SlidingExpiration =>
        SlidingExpirationHelper.GetRandomizedSlidingExpiration(TimeSpan.FromMinutes(5), 30);

    public string BuildCacheKey() => $"{CacheConstants.Stock}:class:{ClassId}:location:{LocationId}";
}

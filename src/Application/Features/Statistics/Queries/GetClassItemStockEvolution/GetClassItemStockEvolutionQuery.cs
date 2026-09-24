using skestock.Application.Common.Caching;
using skestock.Application.Common.Security;
using skestock.Application.Features.Statistics.Models;
using StockCacheConstants = skestock.Application.Features.Stock.CacheConstants;

namespace skestock.Application.Features.Statistics.Queries.GetClassItemStockEvolution;

[Authorize]
public class GetClassItemStockEvolutionQuery : IRequest<Result<ClassItemStockEvolutionDto>>, ICacheableQuery
{
    public Guid ClassId { get; init; }
    public Guid ItemId { get; init; }

    public IReadOnlyCollection<string> Tags =>
    [
        StockCacheConstants.BuildClassTag(ClassId),
        StockCacheConstants.BuildCoarseTag()
    ];

    public bool BypassCache => false;

    public TimeSpan? SlidingExpiration =>
        SlidingExpirationHelper.GetRandomizedSlidingExpiration(TimeSpan.FromMinutes(5), 30);

    public string BuildCacheKey() =>
        $"{CacheConstants.Statistics}:class:{ClassId}:item:{ItemId}:stock-evolution:all-history";
}

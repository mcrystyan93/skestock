using skestock.Application.Common.Caching;
using skestock.Application.Common.Security;
using skestock.Application.Features.Statistics.Models;
using StockCacheConstants = skestock.Application.Features.Stock.CacheConstants;

namespace skestock.Application.Features.Statistics.Queries.GetClassStockByCategory;

[Authorize]
public class GetClassStockByCategoryQuery : IRequest<Result<ClassStockByCategoryDto>>, ICacheableQuery
{
    public Guid ClassId { get; init; }
    public Guid? LocationId { get; init; }

    public IReadOnlyCollection<string> Tags =>
    [
        StockCacheConstants.BuildClassTag(ClassId),
        StockCacheConstants.BuildCoarseTag()
    ];

    public bool BypassCache => false;

    public TimeSpan? SlidingExpiration =>
        SlidingExpirationHelper.GetRandomizedSlidingExpiration(TimeSpan.FromMinutes(5), 30);

    public string BuildCacheKey() =>
        $"{CacheConstants.Statistics}:class:{ClassId}:stock-by-category:" +
        $"location={LocationId?.ToString() ?? "all"}";
}

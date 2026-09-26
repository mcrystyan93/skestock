using skestock.Application.Common.Caching;
using skestock.Application.Common.Security;
using skestock.Application.Features.Statistics.Models;
using StockCacheConstants = skestock.Application.Features.Stock.CacheConstants;

namespace skestock.Application.Features.Statistics.Queries.GetClassStockByCategory;

/// <summary>
/// Class stock per location, split by category. Drill into a single location with
/// <see cref="GetClassLocationStockByItem.GetClassLocationStockByItemQuery"/>.
/// </summary>
[Authorize]
public class GetClassStockByCategoryQuery : IRequest<Result<ClassStockByCategoryDto>>, ICacheableQuery
{
    public Guid ClassId { get; init; }

    public IReadOnlyCollection<string> Tags =>
    [
        StockCacheConstants.BuildClassTag(ClassId),
        StockCacheConstants.BuildCoarseTag()
    ];

    public bool BypassCache => false;

    public TimeSpan? SlidingExpiration =>
        SlidingExpirationHelper.GetRandomizedSlidingExpiration(TimeSpan.FromMinutes(5), 30);

    public string BuildCacheKey() =>
        $"{CacheConstants.Statistics}:class:{ClassId}:stock-by-category";
}

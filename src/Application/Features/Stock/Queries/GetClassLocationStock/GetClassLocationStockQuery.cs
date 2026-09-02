using skestock.Application.Common.Caching;
using skestock.Application.Features.Stock.Models;

namespace skestock.Application.Features.Stock.Queries.GetClassLocationStock;

public class GetClassLocationStockQuery : IRequest<Result<List<StockItemDto>>>, ICacheableQuery<List<StockItemDto>>
{
    public Guid ClassId { get; init; }

    /// <summary>
    /// Optional - when omitted, the report aggregates stock across every location for the class,
    /// with one row per (item, location) combination instead of one row per item.
    /// </summary>
    public Guid? LocationId { get; init; }

    /// <summary>
    /// Optional - when provided, only items whose name contains this term (case-insensitive) are
    /// included in the report.
    /// </summary>
    public string? SearchTerm { get; init; }

    public IReadOnlyCollection<string> Tags =>
        [LocationId is { } locationId ? CacheConstants.BuildTag(ClassId, locationId) : CacheConstants.BuildClassTag(ClassId)];
    public bool BypassCache => false;
    public TimeSpan? SlidingExpiration =>
        SlidingExpirationHelper.GetRandomizedSlidingExpiration(TimeSpan.FromMinutes(5), 30);

    public string BuildCacheKey() =>
        $"{CacheConstants.Stock}:class:{ClassId}:location:{(LocationId is { } locationId ? locationId.ToString() : "all")}:" +
        $"search={CacheKeyNormalization.Text(SearchTerm)}";
}

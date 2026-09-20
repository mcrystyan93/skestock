using skestock.Application.Common.Caching;
using skestock.Application.Common.Filtering;
using skestock.Application.Features.Stock.Models;

namespace skestock.Application.Features.Stock.Queries.GetClassLocationStock;

public class GetClassLocationStockQuery : IRequest<Result<StockReportDto>>, ICacheableQuery
{
    public Guid ClassId { get; init; }

    public List<ColumnFilter> Filters { get; init; } = [];

    /// <summary>
    /// Optional - when provided, only items whose name contains this term (case-insensitive) are
    /// included in the report.
    /// </summary>
    public string? SearchTerm { get; init; }

    public bool IncludeHidden { get; init; }

    public bool LowStockOnly { get; init; }

    public bool ExpiredOnly { get; init; }

    public IReadOnlyCollection<string> Tags =>
    [
        CacheConstants.BuildClassTag(ClassId),
        CacheConstants.BuildCoarseTag()
    ];

    public bool BypassCache => false;

    public TimeSpan? SlidingExpiration =>
        SlidingExpirationHelper.GetRandomizedSlidingExpiration(TimeSpan.FromMinutes(5), 30);

    public string BuildCacheKey() =>
        $"{CacheConstants.Stock}:class:{ClassId}:" +
        $"asOf={DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime):yyyy-MM-dd}:" +
        $"search={CacheKeyNormalization.Text(SearchTerm)}:" +
        $"filters={CacheKeyNormalization.Filters(Filters)}:" +
        $"includeHidden={IncludeHidden}:" +
        $"lowStockOnly={LowStockOnly}:" +
        $"expiredOnly={ExpiredOnly}";
}

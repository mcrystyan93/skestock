using skestock.Application.Common.Caching;
using skestock.Application.Common.Filtering;
using skestock.Application.Common.Models;
using skestock.Application.Features.Items.Models;

namespace skestock.Application.Features.Items.Queries.GetAllItemImportBatches;

public sealed class GetAllItemImportBatchesQuery
    : BasePaginationFilter, IRequest<Result<PaginatedResponse<ItemImportBatchListItemDto>>>, ICacheableQuery
{
    public List<ColumnFilter> Filters { get; init; } = [];

    public IReadOnlyCollection<string> Tags => [CacheConstants.ItemImportBatchListTag];
    public bool BypassCache => false;
    public TimeSpan? SlidingExpiration =>
        SlidingExpirationHelper.GetRandomizedSlidingExpiration(TimeSpan.FromMinutes(5), 30);

    public string BuildCacheKey() =>
        $"{CacheConstants.ItemImportBatch}:list" +
        $"search={CacheKeyNormalization.Text(SearchTerm)}:" +
        $"filters={CacheKeyNormalization.Filters(Filters)}:" +
        $"pageSize={CacheKeyNormalization.Int(PageSize)}:" +
        $"cursor={CacheKeyNormalization.Cursor(Cursor)}:" +
        $"sort={CacheKeyNormalization.Sort(Sort)}";
}

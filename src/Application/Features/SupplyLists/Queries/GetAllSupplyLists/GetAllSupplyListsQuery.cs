using skestock.Application.Common.Caching;
using skestock.Application.Common.Filtering;
using skestock.Application.Common.Models;
using skestock.Application.Features.SupplyLists.Models;

namespace skestock.Application.Features.SupplyLists.Queries.GetAllSupplyLists;

public class GetAllSupplyListsQuery : BasePaginationFilter, IRequest<Result<PaginatedResponse<SupplyListListItemDto>>>, ICacheableQuery
{
    public List<ColumnFilter> Filters { get; init; } = [];
    public IReadOnlyCollection<string> Tags => [CacheConstants.SupplyListListTag];
    public bool BypassCache => false;
    public TimeSpan? SlidingExpiration =>
        SlidingExpirationHelper.GetRandomizedSlidingExpiration(TimeSpan.FromMinutes(5), 30);

    public string BuildCacheKey()
    {
        return $"{CacheConstants.SupplyList}:list:" +
               $"search={CacheKeyNormalization.Text(SearchTerm)}:" +
               $"filters={CacheKeyNormalization.Filters(Filters)}:" +
               $"pageSize={CacheKeyNormalization.Int(PageSize)}:" +
               $"cursor={CacheKeyNormalization.Cursor(Cursor)}:" +
               $"sort={CacheKeyNormalization.Sort(Sort)}";
    }
}

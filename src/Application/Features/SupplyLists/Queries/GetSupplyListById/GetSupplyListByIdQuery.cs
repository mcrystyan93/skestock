using skestock.Application.Common.Caching;
using skestock.Application.Features.SupplyLists.Models;

namespace skestock.Application.Features.SupplyLists.Queries.GetSupplyListById;

public class GetSupplyListByIdQuery : IRequest<Result<SupplyListDto>>, ICacheableQuery
{
    public Guid Id { get; init; }

    // The DTO embeds item and category names, so item/category changes must evict it too.
    public IReadOnlyCollection<string> Tags =>
        [CacheConstants.SupplyListTag(Id), Items.CacheConstants.ItemListTag, Categories.CacheConstants.CategoryListTag];
    public bool BypassCache => false;
    public TimeSpan? SlidingExpiration =>
        SlidingExpirationHelper.GetRandomizedSlidingExpiration(TimeSpan.FromMinutes(5), 30);

    public string BuildCacheKey() => $"{CacheConstants.SupplyList}:{Id:N}";
}

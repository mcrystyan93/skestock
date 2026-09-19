using skestock.Application.Common.Caching;
using skestock.Application.Features.Items.Models;

namespace skestock.Application.Features.Items.Queries.GetItemById;

public class GetItemByIdQuery : IRequest<Result<ItemDto>>, ICacheableQuery
{
    public Guid Id { get; init; }

    public IReadOnlyCollection<string> Tags => [CacheConstants.ItemListTag, CacheConstants.ItemTag(Id)];
    public bool BypassCache => false;
    public TimeSpan? SlidingExpiration =>
        SlidingExpirationHelper.GetRandomizedSlidingExpiration(TimeSpan.FromMinutes(5), 30);

    public string BuildCacheKey() => $"{CacheConstants.Item}:{Id:N}";
}

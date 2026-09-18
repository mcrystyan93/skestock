using skestock.Application.Common.Caching;
using skestock.Application.Features.OrderLists.Models;

namespace skestock.Application.Features.OrderLists.Queries.GetOrderListById;

public class GetOrderListByIdQuery : IRequest<Result<OrderListDto>>, ICacheableQuery
{
    public Guid Id { get; init; }

    public IReadOnlyCollection<string> Tags => [CacheConstants.OrderListListTag, CacheConstants.OrderListTag(Id)];
    public bool BypassCache => false;
    public TimeSpan? SlidingExpiration =>
        SlidingExpirationHelper.GetRandomizedSlidingExpiration(TimeSpan.FromMinutes(5), 30);

    public string BuildCacheKey() => $"{CacheConstants.OrderList}:{Id:N}";
}

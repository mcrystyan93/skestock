using skestock.Application.Common.Caching;
using skestock.Application.Common.Security;

namespace skestock.Application.Features.OrderLists.Commands.DeleteOrderList;

[Authorize]
public class DeleteOrderListCommand : IRequest<Result>, ICacheInvalidation
{
    public Guid Id { get; init; }

    public IReadOnlyCollection<string> Tags =>
        [CacheConstants.OrderListListTag, CacheConstants.OrderListTag(Id)];
}

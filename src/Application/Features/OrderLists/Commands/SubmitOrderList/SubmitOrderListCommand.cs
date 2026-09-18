using skestock.Application.Common.Caching;
using skestock.Application.Common.Security;
using skestock.Application.Features.OrderLists.Models;

namespace skestock.Application.Features.OrderLists.Commands.SubmitOrderList;

[Authorize]
public class SubmitOrderListCommand : IRequest<Result<OrderListDto>>, ICacheInvalidation
{
    public Guid Id { get; init; }

    public IReadOnlyCollection<string> Tags =>
        [CacheConstants.OrderListListTag, CacheConstants.OrderListTag(Id)];
}

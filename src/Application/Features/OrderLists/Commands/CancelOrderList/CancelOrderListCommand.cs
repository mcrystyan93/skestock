using skestock.Application.Common.Behaviours;
using skestock.Application.Common.Caching;
using skestock.Application.Common.Security;
using skestock.Application.Features.OrderLists.Models;

namespace skestock.Application.Features.OrderLists.Commands.CancelOrderList;

[Authorize]
public class CancelOrderListCommand : IRequest<Result<OrderListDto>>, ICacheInvalidation, IRequiresOpenClass
{
    Guid? IRequiresOpenClass.OrderListId => Id;

    public Guid Id { get; init; }

    public IReadOnlyCollection<string> Tags =>
        [CacheConstants.OrderListListTag, CacheConstants.OrderListTag(Id)];
}

using skestock.Application.Common.Caching;
using skestock.Application.Common.Security;
using skestock.Application.Features.OrderLists.Models;

namespace skestock.Application.Features.OrderLists.Commands.AddItemToOrderList;

[Authorize]
public class AddItemToOrderListCommand : IRequest<Result<OrderListDto>>, ICacheInvalidation
{
    public Guid ClassId { get; init; }
    public Guid ItemId { get; init; }
    public decimal Quantity { get; init; }

    // Exactly one of OrderListId / NewOrderListName must be provided.
    public Guid? OrderListId { get; init; }
    public string? NewOrderListName { get; init; }

    public IReadOnlyCollection<string> Tags => OrderListId is { } id
        ? [CacheConstants.OrderListListTag, CacheConstants.OrderListTag(id)]
        : [CacheConstants.OrderListListTag];
}

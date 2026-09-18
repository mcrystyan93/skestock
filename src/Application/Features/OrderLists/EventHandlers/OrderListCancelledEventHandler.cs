using Microsoft.Extensions.Caching.Hybrid;
using skestock.Domain.Events.OrderList;

namespace skestock.Application.Features.OrderLists.EventHandlers;

public class OrderListCancelledEventHandler(HybridCache cache)
    : INotificationHandler<OrderListCancelledEvent>
{
    public async ValueTask Handle(OrderListCancelledEvent notification, CancellationToken cancellationToken)
    {
        await cache.RemoveByTagAsync(
            [CacheConstants.OrderListListTag, CacheConstants.OrderListTag(notification.OrderListId)],
            cancellationToken);
    }
}

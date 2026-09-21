using Microsoft.Extensions.Caching.Hybrid;
using skestock.Domain.Events.OrderList;

namespace skestock.Application.Features.OrderLists.EventHandlers;

public class OrderListReopenedEventHandler(HybridCache cache)
    : INotificationHandler<OrderListReopenedEvent>
{
    public async ValueTask Handle(OrderListReopenedEvent notification, CancellationToken cancellationToken)
    {
        await cache.RemoveByTagAsync(
            [CacheConstants.OrderListListTag, CacheConstants.OrderListTag(notification.OrderListId)],
            cancellationToken);
    }
}

using Microsoft.Extensions.Caching.Hybrid;
using skestock.Domain.Events.OrderList;

namespace skestock.Application.Features.OrderLists.EventHandlers;

public class OrderListSubmittedEventHandler(HybridCache cache)
    : INotificationHandler<OrderListSubmittedEvent>
{
    public async ValueTask Handle(OrderListSubmittedEvent notification, CancellationToken cancellationToken)
    {
        await cache.RemoveByTagAsync(
            [CacheConstants.OrderListListTag, CacheConstants.OrderListTag(notification.OrderListId)],
            cancellationToken);
    }
}

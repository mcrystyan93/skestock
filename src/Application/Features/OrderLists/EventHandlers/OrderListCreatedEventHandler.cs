using Microsoft.Extensions.Caching.Hybrid;
using skestock.Domain.Events.OrderList;

namespace skestock.Application.Features.OrderLists.EventHandlers;

public class OrderListCreatedEventHandler(HybridCache cache)
    : INotificationHandler<OrderListCreatedEvent>
{
    public async ValueTask Handle(OrderListCreatedEvent notification, CancellationToken cancellationToken)
    {
        await cache.RemoveByTagAsync(
            [CacheConstants.OrderListListTag, CacheConstants.OrderListTag(notification.OrderListId)],
            cancellationToken);
    }
}

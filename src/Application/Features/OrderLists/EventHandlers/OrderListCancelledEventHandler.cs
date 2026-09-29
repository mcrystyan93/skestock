using Microsoft.Extensions.Caching.Hybrid;
using skestock.Application.Common.Interfaces;
using skestock.Application.Common.Realtime;
using skestock.Domain.Events.OrderList;

namespace skestock.Application.Features.OrderLists.EventHandlers;

public class OrderListCancelledEventHandler(IRealtimeNotifier notifier, HybridCache cache)
    : INotificationHandler<OrderListCancelledEvent>
{
    public async ValueTask Handle(OrderListCancelledEvent notification, CancellationToken cancellationToken)
    {
        await cache.RemoveByTagAsync(
            [CacheConstants.OrderListListTag, CacheConstants.OrderListTag(notification.OrderListId)],
            cancellationToken);

        await notifier.NotifyGroupAsync(
            RealtimeGroups.SchoolClass(notification.ClassId),
            RealtimeEvents.OrderListCancelled,
            new { OrderListId = notification.OrderListId, ClassId = notification.ClassId },
            cancellationToken);
    }
}

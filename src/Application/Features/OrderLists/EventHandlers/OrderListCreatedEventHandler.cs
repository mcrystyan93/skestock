using Microsoft.Extensions.Caching.Hybrid;
using skestock.Application.Common.Interfaces;
using skestock.Application.Common.Realtime;
using skestock.Domain.Events.OrderList;

namespace skestock.Application.Features.OrderLists.EventHandlers;

public class OrderListCreatedEventHandler(IRealtimeNotifier notifier, HybridCache cache)
    : INotificationHandler<OrderListCreatedEvent>
{
    public async ValueTask Handle(OrderListCreatedEvent notification, CancellationToken cancellationToken)
    {
        await cache.RemoveByTagAsync(
            [CacheConstants.OrderListListTag, CacheConstants.OrderListTag(notification.OrderListId)],
            cancellationToken);

        await notifier.NotifyGroupAsync(
            RealtimeGroups.SchoolClass(notification.ClassId),
            RealtimeEvents.OrderListCreated,
            new { OrderListId = notification.OrderListId, ClassId = notification.ClassId },
            cancellationToken);
    }
}

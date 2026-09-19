using Microsoft.Extensions.Caching.Hybrid;
using skestock.Application.Common.Interfaces;
using skestock.Application.Common.Realtime;
using skestock.Domain.Events.Stock;

namespace skestock.Application.Features.Stock.EventHandlers;

public sealed class ClassItemStockVisibilityChangedEventHandler(
    IRealtimeNotifier notifier,
    HybridCache cache) : INotificationHandler<ClassItemStockVisibilityChangedEvent>
{
    public async ValueTask Handle(
        ClassItemStockVisibilityChangedEvent notification,
        CancellationToken cancellationToken)
    {
        await cache.RemoveByTagAsync(
            [
                CacheConstants.BuildCoarseTag(),
                CacheConstants.BuildClassTag(notification.ClassId)
            ],
            cancellationToken);

        await notifier.NotifyGroupAsync(
            RealtimeGroups.SchoolClass(notification.ClassId),
            RealtimeEvents.ClassItemStockVisibilityChanged,
            new
            {
                classId = notification.ClassId,
                itemId = notification.ItemId,
                locationId = notification.LocationId,
                hideWhenZeroStock = notification.HideWhenZeroStock
            },
            cancellationToken);
    }
}

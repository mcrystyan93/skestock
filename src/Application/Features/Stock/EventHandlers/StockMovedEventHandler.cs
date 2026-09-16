using Microsoft.Extensions.Caching.Hybrid;
using skestock.Application.Common.Interfaces;
using skestock.Application.Common.Realtime;
using skestock.Domain.Events.Stock;
using StockBatchCacheConstants = skestock.Application.Features.StockBatches.CacheConstants;

namespace skestock.Application.Features.Stock.EventHandlers;

public sealed class StockMovedEventHandler(IRealtimeNotifier notifier, HybridCache cache)
    : INotificationHandler<StockMovedEvent>
{
    public async ValueTask Handle(StockMovedEvent notification, CancellationToken cancellationToken)
    {
        var tags = new[]
        {
            CacheConstants.BuildCoarseTag(), CacheConstants.BuildClassTag(notification.ClassId),
            CacheConstants.BuildTag(notification.ClassId, notification.SourceLocationId),
            CacheConstants.BuildTag(notification.ClassId, notification.DestinationLocationId),
            StockBatchCacheConstants.StockBatchListTag
        };

        await cache.RemoveByTagAsync(tags, cancellationToken);

        await notifier.NotifyGroupAsync(
            RealtimeGroups.SchoolClass(notification.ClassId),
            RealtimeEvents.StockMoved,
            new
            {
                classId = notification.ClassId,
                itemId = notification.ItemId,
                sourceLocationId = notification.SourceLocationId,
                destinationLocationId = notification.DestinationLocationId,
                quantity = notification.Quantity
            },
            cancellationToken);
    }
}

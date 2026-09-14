using Microsoft.Extensions.Caching.Hybrid;
using skestock.Application.Common.Interfaces;
using skestock.Application.Common.Realtime;
using skestock.Domain.Events.StockBatches;
using StockCacheConstants = skestock.Application.Features.Stock.CacheConstants;

namespace skestock.Application.Features.StockBatches.EventHandlers;

public sealed class StockBatchCreatedEventHandler(IRealtimeNotifier notifier, HybridCache cache)
    : INotificationHandler<StockBatchCreatedEvent>
{
    public async ValueTask Handle(StockBatchCreatedEvent notification, CancellationToken cancellationToken)
    {
        var tags = new[]
        {
            StockCacheConstants.BuildCoarseTag(),
            StockCacheConstants.BuildClassTag(notification.ClassId),
            StockCacheConstants.BuildTag(notification.ClassId, notification.LocationId),
            CacheConstants.StockBatchListTag
        };

        await cache.RemoveByTagAsync(tags, cancellationToken);

        await notifier.NotifyGroupAsync(
            RealtimeGroups.SchoolClass(notification.ClassId),
            RealtimeEvents.StockBatchCreated,
            new { ClassId = notification.ClassId, LocationId = notification.LocationId },
            cancellationToken);
    }
}

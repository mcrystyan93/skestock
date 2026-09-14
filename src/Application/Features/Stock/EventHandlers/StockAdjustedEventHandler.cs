using Microsoft.Extensions.Caching.Hybrid;
using skestock.Application.Common.Interfaces;
using skestock.Application.Common.Realtime;
using skestock.Domain.Events.Stock;
using StockBatchCacheConstants = skestock.Application.Features.StockBatches.CacheConstants;

namespace skestock.Application.Features.Stock.EventHandlers;

public sealed class StockAdjustedEventHandler(IRealtimeNotifier notifier, HybridCache cache)
    : INotificationHandler<StockAdjustedEvent>
{
    public async ValueTask Handle(StockAdjustedEvent notification, CancellationToken cancellationToken)
    {
        var tags = new[]
        {
            CacheConstants.BuildCoarseTag(),
            CacheConstants.BuildClassTag(notification.ClassId),
            CacheConstants.BuildTag(notification.ClassId, notification.LocationId),
            StockBatchCacheConstants.StockBatchListTag
        };

        await cache.RemoveByTagAsync(tags, cancellationToken);

        await notifier.NotifyGroupAsync(
            RealtimeGroups.SchoolClass(notification.ClassId),
            RealtimeEvents.StockAdjusted,
            new { ClassId = notification.ClassId, LocationId = notification.LocationId },
            cancellationToken);
    }
}

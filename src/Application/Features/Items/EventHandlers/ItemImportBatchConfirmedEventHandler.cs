using Microsoft.Extensions.Caching.Hybrid;
using skestock.Application.Common.Interfaces;
using skestock.Application.Common.Realtime;
using skestock.Domain.Events.Items;
using StockCacheConstants = skestock.Application.Features.Stock.CacheConstants;

namespace skestock.Application.Features.Items.EventHandlers;

public class ItemImportBatchConfirmedEventHandler(IRealtimeNotifier notifier, HybridCache cache)
    : INotificationHandler<ItemImportBatchConfirmedEvent>
{
    private readonly IReadOnlyCollection<string> _tags =
    [
        CacheConstants.ItemImportBatchListTag,
        CacheConstants.ItemListTag,
        StockCacheConstants.BuildCoarseTag()
    ];

    public async ValueTask Handle(ItemImportBatchConfirmedEvent notification, CancellationToken cancellationToken)
    {
        await cache.RemoveByTagAsync(_tags, cancellationToken);

        var payload = new { ItemImportBatchId = notification.BatchId };

        await notifier.NotifyGroupAsync(
            RealtimeGroups.ItemImportBatchesList,
            RealtimeEvents.ItemImportBatchConfirmed,
            payload,
            cancellationToken);
    }
}

using Microsoft.Extensions.Caching.Hybrid;
using skestock.Application.Common.Interfaces;
using skestock.Application.Common.Realtime;
using skestock.Domain.Events.Items;
using StockCacheConstants = skestock.Application.Features.Stock.CacheConstants;

namespace skestock.Application.Features.Items.EventHandlers;

public class ItemImportConfirmedEventHandler(IRealtimeNotifier notifier, HybridCache cache)
    : INotificationHandler<ItemImportConfirmedEvent>
{
    private readonly IReadOnlyCollection<string> _tags =
    [
        CacheConstants.ItemImportListTag,
        CacheConstants.ItemListTag,
        StockCacheConstants.BuildCoarseTag()
    ];

    public async ValueTask Handle(ItemImportConfirmedEvent notification, CancellationToken cancellationToken)
    {
        await cache.RemoveByTagAsync(_tags, cancellationToken);

        var payload = new { ItemImportId = notification.ImportId };

        await notifier.NotifyGroupAsync(
            RealtimeGroups.ItemImportsList,
            RealtimeEvents.ItemImportConfirmed,
            payload,
            cancellationToken);

        await notifier.NotifyGroupAsync(
            RealtimeGroups.ItemsList,
            RealtimeEvents.ItemImportConfirmed,
            payload,
            cancellationToken);
    }
}

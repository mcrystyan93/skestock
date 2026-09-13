using Microsoft.Extensions.Caching.Hybrid;
using skestock.Application.Common.Interfaces;
using skestock.Application.Common.Realtime;
using skestock.Domain.Events.Items;

namespace skestock.Application.Features.Items.EventHandlers;

public class ItemImportBatchFailedEventHandler(IRealtimeNotifier notifier, HybridCache cache)
    : INotificationHandler<ItemImportBatchFailedEvent>
{
    private readonly IReadOnlyCollection<string> _tags =
    [
        CacheConstants.ItemImportBatchListTag
    ];

    public async ValueTask Handle(ItemImportBatchFailedEvent notification, CancellationToken cancellationToken)
    {
        await cache.RemoveByTagAsync(_tags, cancellationToken);

        var payload = new { ItemImportBatchId = notification.BatchId };

        await notifier.NotifyGroupAsync(
            RealtimeGroups.ItemImportBatchesList,
            RealtimeEvents.ItemImportBatchProcessed,
            payload,
            cancellationToken);
    }
}

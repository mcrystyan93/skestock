using Microsoft.Extensions.Caching.Hybrid;
using skestock.Application.Common.Interfaces;
using skestock.Application.Common.Realtime;
using skestock.Domain.Events.Items;

namespace skestock.Application.Features.Items.EventHandlers;

public class ItemImportBatchCompletedEventHandler(IRealtimeNotifier notifier, HybridCache cache)
    : INotificationHandler<ItemImportBatchCompletedEvent>
{
    private readonly IReadOnlyCollection<string> _tags =
    [
        CacheConstants.ItemImportBatchListTag
    ];

    public async ValueTask Handle(ItemImportBatchCompletedEvent notification, CancellationToken cancellationToken)
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

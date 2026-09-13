using Microsoft.Extensions.Caching.Hybrid;
using skestock.Application.Common.Interfaces;
using skestock.Application.Common.Realtime;
using skestock.Domain.Events.Items;

namespace skestock.Application.Features.Items.EventHandlers;

public class ItemImportBatchCreatedEventHandler(IRealtimeNotifier notifier, HybridCache cache)
    : INotificationHandler<ItemImportBatchCreatedEvent>
{
    private readonly IReadOnlyCollection<string> _tags =
    [
        CacheConstants.ItemImportBatchListTag
    ];

    public async ValueTask Handle(ItemImportBatchCreatedEvent notification, CancellationToken cancellationToken)
    {
        await cache.RemoveByTagAsync(_tags, cancellationToken);

        var payload = new { ItemImportBatchId = notification.Batch.Id };
        await notifier.NotifyGroupAsync(
            RealtimeGroups.ItemImportBatchesList,
            RealtimeEvents.ItemImportBatchCreated,
            payload,
            cancellationToken);
    }
}

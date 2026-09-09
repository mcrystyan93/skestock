using Microsoft.Extensions.Caching.Hybrid;
using skestock.Application.Common.Interfaces;
using skestock.Application.Common.Realtime;
using skestock.Domain.Events.Items;

namespace skestock.Application.Features.Items.EventHandlers;

public class ItemImportFailedEventHandler(IRealtimeNotifier notifier, HybridCache cache)
    : INotificationHandler<ItemImportFailedEvent>
{
    private readonly IReadOnlyCollection<string> _tags =
    [
        CacheConstants.ItemImportListTag
    ];

    public async ValueTask Handle(ItemImportFailedEvent notification, CancellationToken cancellationToken)
    {
        await cache.RemoveByTagAsync(_tags, cancellationToken);

        var payload = new { ItemImportId = notification.ImportId };

        await notifier.NotifyGroupAsync(
            RealtimeGroups.ItemImportsList,
            RealtimeEvents.ItemImportProcessed,
            payload,
            cancellationToken);
    }
}

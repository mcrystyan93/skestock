using Microsoft.Extensions.Caching.Hybrid;
using skestock.Application.Common.Interfaces;
using skestock.Application.Common.Realtime;
using skestock.Domain.Events.Items;

namespace skestock.Application.Features.Items.EventHandlers;

public class ItemImportCreatedEventHandler(
    IRealtimeNotifier notifier,
    HybridCache cache)
    : INotificationHandler<ItemImportCreatedEvent>
{
    private readonly IReadOnlyCollection<string> _tags =
    [
        CacheConstants.ItemImportListTag
    ];

    public async ValueTask Handle(ItemImportCreatedEvent notification, CancellationToken cancellationToken)
    {
        await cache.RemoveByTagAsync(_tags, cancellationToken);

        var payload = new { ItemImportId = notification.Import.Id };
        await notifier.NotifyGroupAsync(
            RealtimeGroups.ItemImportsList,
            RealtimeEvents.ItemImportCreated,
            payload,
            cancellationToken);
    }
}

using Microsoft.Extensions.Caching.Hybrid;
using skestock.Application.Common.Interfaces;
using skestock.Application.Common.Realtime;
using skestock.Domain.Events.GoodsReceipt;

namespace skestock.Application.Features.GoodsReceipts.EventHandlers;

public class GoodsReceiptImportCreatedEventHandler(
    IRealtimeNotifier notifier,
    HybridCache cache)
    : INotificationHandler<GoodsReceiptImportCreatedEvent>
{
    private readonly IReadOnlyCollection<string> _tags =
    [
        CacheConstants.GoodsReceiptImportListTag
    ];

    public async ValueTask Handle(GoodsReceiptImportCreatedEvent notification, CancellationToken cancellationToken)
    {
        await cache.RemoveByTagAsync(_tags, cancellationToken);

        var payload = new { GoodsReceiptImportId = notification.Import.Id };
        await notifier.NotifyGroupAsync(
            RealtimeGroups.GoodsReceiptImportsList,
            RealtimeEvents.GoodsReceiptImportCreated,
            payload,
            cancellationToken);
    }
}

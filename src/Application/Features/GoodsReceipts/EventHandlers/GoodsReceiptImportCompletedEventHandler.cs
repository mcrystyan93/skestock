using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using skestock.Application.Common.Interfaces;
using skestock.Application.Common.Realtime;
using skestock.Domain.Enums;
using skestock.Domain.Events.GoodsReceipt;

namespace skestock.Application.Features.GoodsReceipts.EventHandlers;

public class GoodsReceiptImportCompletedEventHandler(IRealtimeNotifier notifier, HybridCache cache)
    : INotificationHandler<GoodsReceiptImportCompletedEvent>
{
    private readonly IReadOnlyCollection<string> _tags =
    [
        CacheConstants.GoodsReceiptImportListTag,
    ];

    public async ValueTask Handle(GoodsReceiptImportCompletedEvent notification, CancellationToken cancellationToken)
    {
        await cache.RemoveByTagAsync(_tags, cancellationToken);
        
        var payload = new { GoodsReceiptImportId = notification.ImportId };

        await notifier.NotifyGroupAsync(
            RealtimeGroups.GoodsReceiptImportsList,
            RealtimeEvents.GoodsReceiptImportProcessed,
            payload,
            cancellationToken);
    }
}

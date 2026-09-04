using Microsoft.Extensions.Logging;
using skestock.Application.Common.Interfaces;
using skestock.Domain.Enums;
using skestock.Domain.Events.GoodsReceipt;

namespace skestock.Application.Features.GoodsReceipts.EventHandlers;

public class GoodsReceiptImportCompletedEventHandler(IRealtimeNotifier notifier)
    : INotificationHandler<GoodsReceiptImportCompletedEvent>
{
    public async ValueTask Handle(GoodsReceiptImportCompletedEvent notification, CancellationToken cancellationToken)
    {
        var payload = new { GoodsReceiptImportId = notification.ImportId };

        await Task.WhenAll([
            notifier.NotifyGroupAsync("goods-receipts-import-list", "GoodsReceiptImportCompletedMarkListChanged", payload,
                cancellationToken),
            notifier.NotifyUserAsync(notification.UploadedByUserId.ToString(), "GoodsReceiptImportCompletedNotifyUser", payload,
                cancellationToken)
        ]);
    }
}

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

        await notifier.NotifyGroupAsync("goods-receipts-import-list", "GoodsReceiptImportProcessed", payload,
            cancellationToken);
    }
}

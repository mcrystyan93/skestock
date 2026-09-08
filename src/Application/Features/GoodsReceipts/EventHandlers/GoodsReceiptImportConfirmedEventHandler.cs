using Microsoft.Extensions.Caching.Hybrid;
using skestock.Application.Common.Interfaces;
using skestock.Domain.Events.GoodsReceipt;
using StockCacheConstants = skestock.Application.Features.Stock.CacheConstants;

namespace skestock.Application.Features.GoodsReceipts.EventHandlers;

public class GoodsReceiptImportConfirmedEventHandler(IRealtimeNotifier notifier, HybridCache cache)
    : INotificationHandler<GoodsReceiptImportConfirmedEvent>
{
    private readonly IReadOnlyCollection<string> _tags =
    [
        CacheConstants.GoodsReceiptImportListTag, 
        CacheConstants.GoodsReceiptListTag,
        StockCacheConstants.BuildCoarseTag()
    ];
    public async ValueTask Handle(GoodsReceiptImportConfirmedEvent notification, CancellationToken cancellationToken)
    {
        await cache.RemoveByTagAsync(_tags, cancellationToken);
        
        var payload = new { GoodsReceiptImportId = notification.ImportId };

        await notifier.NotifyGroupAsync("goods-receipts-import-list", "GoodsReceiptImportConfirmed", payload,
            cancellationToken);
    }
}

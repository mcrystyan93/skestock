using skestock.Domain.Events.GoodsReceipt;

namespace skestock.Application.Features.GoodsReceipts.EventHandlers;

public class GoodsReceiptImportFailedEventHandler: INotificationHandler<GoodsReceiptImportFailedEvent>
{
    public ValueTask Handle(GoodsReceiptImportFailedEvent notification, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}

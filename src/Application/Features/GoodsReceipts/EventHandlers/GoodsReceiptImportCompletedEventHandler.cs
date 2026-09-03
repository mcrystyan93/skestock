using Microsoft.Extensions.Logging;
using skestock.Application.Common.Interfaces;
using skestock.Domain.Enums;
using skestock.Domain.Events.GoodsReceipt;

namespace skestock.Application.Features.GoodsReceipts.EventHandlers;

public class GoodsReceiptImportCompletedEventHandler(
    IApplicationDbContext dbContext,
    ILogger<GoodsReceiptImportCompletedEventHandler> logger) : INotificationHandler<GoodsReceiptImportCompletedEvent>
{
    public async ValueTask Handle(GoodsReceiptImportCompletedEvent notification, CancellationToken cancellationToken)
    {
        var import = await dbContext.GoodsReceiptImports
            .FirstOrDefaultAsync(x => x.Id == notification.ImportId, cancellationToken);

        if (import is null)
        {
            logger.LogError("Goods receipt import not found: {GoodsReceiptImportId}", notification.ImportId);
            return;
        }

        // make sure it's not processed already
        if (import.Status is GoodsReceiptImportStatus.Confirmed or GoodsReceiptImportStatus.Failed
            or GoodsReceiptImportStatus.PendingReview)
        {
            logger.LogInformation("Goods receipt import already processed: {GoodsReceiptImportId}, Status: {Status}",
                notification.ImportId, import.Status);
            return;
        }
        
        // waiting for user to review the import
        import.Status = GoodsReceiptImportStatus.PendingReview;
        
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

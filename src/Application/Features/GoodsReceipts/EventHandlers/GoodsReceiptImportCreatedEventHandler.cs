using skestock.Application.Common.Interfaces;
using skestock.Application.Features.GoodsReceipts.Commands.ProcessGoodsReceiptImport;
using skestock.Domain.Events.GoodsReceipt;
using skestock.Domain.Queues;
using skestock.Shared;

namespace skestock.Application.Features.GoodsReceipts.EventHandlers;

public class GoodsReceiptImportCreatedEventHandler(IApplicationDbContext dbContext)
    : INotificationHandler<GoodsReceiptImportCreatedEvent>
{
    public ValueTask Handle(GoodsReceiptImportCreatedEvent notification, CancellationToken cancellationToken)
    {
        dbContext.OutboxMessages.Add(new OutboxMessage()
        {
            Type = typeof(ProcessGoodsReceiptImportCommand).AssemblyQualifiedName!,
            Payload = System.Text.Json.JsonSerializer.Serialize(
                new ProcessGoodsReceiptImportCommand(notification.Import.Id)),
            QueueName = Services.GoodsReceiptImportQueue,
            UserId = notification.Import.UploadedByUserId
        });

        return ValueTask.CompletedTask;
    }
}

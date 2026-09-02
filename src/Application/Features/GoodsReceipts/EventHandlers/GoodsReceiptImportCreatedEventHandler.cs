using skestock.Application.Common.Interfaces;
using skestock.Application.Features.GoodsReceipts.Messages;
using skestock.Domain.Events.GoodsReceipt;
using skestock.Domain.Queues;
using skestock.Shared;

namespace skestock.Application.Features.GoodsReceipts.EventHandlers;

public class GoodsReceiptImportCreatedEventHandler(IApplicationDbContext dbContext): INotificationHandler<GoodsReceiptImportCreatedEvent>
{
    public ValueTask Handle(GoodsReceiptImportCreatedEvent notification, CancellationToken cancellationToken)
    {
        dbContext.OutboxMessages.Add(new OutboxMessage()
        {
            Type = typeof(ProcessGoodsReceiptImportMessage).AssemblyQualifiedName!,
            Payload = System.Text.Json.JsonSerializer.Serialize(
                new ProcessGoodsReceiptImportMessage(notification.Import.Id)),
            QueueName = Services.GoodsReceiptImportQueue
        });

        return ValueTask.CompletedTask;
    }
}

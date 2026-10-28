using System.Text.Json;
using skestock.Application.Features.Items.Commands.GenerateItemThumbnail;
using skestock.Domain.Queues;
using skestock.Shared;

namespace skestock.Application.Features.Items;

public static class ItemImageOutbox
{
    public static OutboxMessage ThumbnailMessage(Guid itemId, Guid imageFileId, Guid? userId) => new()
    {
        Type = typeof(GenerateItemThumbnailCommand).AssemblyQualifiedName!,
        Payload = JsonSerializer.Serialize(new GenerateItemThumbnailCommand(itemId, imageFileId)),
        QueueName = Services.ItemImageThumbnailQueue,
        UserId = userId
    };
}

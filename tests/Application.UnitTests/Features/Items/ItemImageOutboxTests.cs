using System.Text.Json;
using NUnit.Framework;
using Shouldly;
using skestock.Application.Features.Items;
using skestock.Application.Features.Items.Commands.GenerateItemThumbnail;
using skestock.Shared;

namespace skestock.Application.UnitTests.Features.Items;

[TestFixture]
public class ItemImageOutboxTests
{
    [Test]
    public void ThumbnailMessage_BuildsQueueMessage()
    {
        var itemId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var message = ItemImageOutbox.ThumbnailMessage(itemId, fileId, userId);

        message.Type.ShouldBe(typeof(GenerateItemThumbnailCommand).AssemblyQualifiedName);
        message.QueueName.ShouldBe(Services.ItemImageThumbnailQueue);
        message.UserId.ShouldBe(userId);
        JsonSerializer.Deserialize<GenerateItemThumbnailCommand>(message.Payload)
            .ShouldBe(new GenerateItemThumbnailCommand(itemId, fileId));
    }
}

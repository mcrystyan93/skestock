using NUnit.Framework;
using Shouldly;
using skestock.Domain.Enums;
using skestock.Infrastructure.FruitDessert;

namespace skestock.Application.UnitTests.Infrastructure.FruitDessert;

public class WhatsAppLinkSenderTests
{
    [Test]
    public async Task SendAsync_BuildsWaMeLinkWithEncodedBody_AndOpenedStatus()
    {
        var sender = new WhatsAppLinkSender();

        var result = await sender.SendAsync("40722123456", "Mulțumim, Ștefan!\nLa mulți ani", CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Status.ShouldBe(FruitDessertMessageStatus.Opened);
        result.Value.Url.ShouldBe(
            "https://wa.me/40722123456?text=Mul%C8%9Bumim%2C%20%C8%98tefan%21%0ALa%20mul%C8%9Bi%20ani");
    }
}

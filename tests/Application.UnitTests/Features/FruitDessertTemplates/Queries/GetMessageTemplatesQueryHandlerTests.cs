using NUnit.Framework;
using Shouldly;
using skestock.Application.Features.FruitDessertTemplates;
using skestock.Application.Features.FruitDessertTemplates.Queries.GetMessageTemplates;
using skestock.Application.UnitTests.Features.FruitDessertPlans;
using skestock.Domain.Entities.FruitDessert;
using skestock.Domain.Enums;

namespace skestock.Application.UnitTests.Features.FruitDessertTemplates.Queries;

public class GetMessageTemplatesQueryHandlerTests
{
    [Test]
    public async Task Handle_ReturnsActiveTemplatesOrderedByType_AndCatalogPlaceholders()
    {
        await using var context = FruitDessertTestDbContext.Create();
        context.MessageTemplates.AddRange(
            new MessageTemplate { Type = FruitDessertMessageType.Thanks, Name = "T", Body = "<p>t</p>", IsActive = true },
            new MessageTemplate { Type = FruitDessertMessageType.Reminder, Name = "R", Body = "<p>r</p>", IsActive = true },
            new MessageTemplate { Type = FruitDessertMessageType.Reminder, Name = "Old", Body = "old", IsActive = false });
        await context.SaveChangesAsync();

        var result = await new GetMessageTemplatesQueryHandler(context)
            .Handle(new GetMessageTemplatesQuery(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Templates.Select(t => t.Type).ShouldBe([FruitDessertMessageType.Reminder, FruitDessertMessageType.Thanks]);
        result.Value.Templates.Select(t => t.Name).ShouldBe(["R", "T"]);
        result.Value.Placeholders.Select(p => p.Token)
            .ShouldBe(MessagePlaceholders.All.Select(p => p.Token));
    }
}

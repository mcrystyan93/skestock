using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;
using skestock.Application.Common.Errors;
using skestock.Application.Features.FruitDessertTemplates.Commands.UpdateMessageTemplate;
using skestock.Application.UnitTests.Features.FruitDessertPlans;
using skestock.Domain.Entities.FruitDessert;
using skestock.Domain.Enums;

namespace skestock.Application.UnitTests.Features.FruitDessertTemplates.Commands;

public class UpdateMessageTemplateCommandHandlerTests
{
    [Test]
    public async Task Handle_SanitizesBodyAndUpdatesInPlace()
    {
        await using var context = FruitDessertTestDbContext.Create();
        context.MessageTemplates.Add(new MessageTemplate
            { Type = FruitDessertMessageType.Reminder, Name = "Vechi", Body = "vechi", IsActive = true });
        await context.SaveChangesAsync();

        var result = await new UpdateMessageTemplateCommandHandler(context).Handle(
            new UpdateMessageTemplateCommand(FruitDessertMessageType.Reminder, "Nou",
                "<p onclick=\"x()\">Salut <script>alert(1)</script><strong>{ContactPerson}</strong></p>"),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        var row = await context.MessageTemplates.SingleAsync();
        row.Name.ShouldBe("Nou");
        row.Body.ShouldBe("<p>Salut <strong>{ContactPerson}</strong></p>");
    }

    [Test]
    public async Task Handle_NoActiveTemplate_ReturnsActiveTemplateMissing()
    {
        await using var context = FruitDessertTestDbContext.Create();
        context.MessageTemplates.Add(new MessageTemplate
            { Type = FruitDessertMessageType.Reminder, Name = "x", Body = "x", IsActive = false });
        await context.SaveChangesAsync();

        var result = await new UpdateMessageTemplateCommandHandler(context).Handle(
            new UpdateMessageTemplateCommand(FruitDessertMessageType.Reminder, "Nou", "<p>n</p>"),
            CancellationToken.None);

        result.Errors.ShouldContain(e => e is FruitDessertErrors.ActiveTemplateMissing);
        (await context.MessageTemplates.SingleAsync()).Name.ShouldBe("x");
    }
}

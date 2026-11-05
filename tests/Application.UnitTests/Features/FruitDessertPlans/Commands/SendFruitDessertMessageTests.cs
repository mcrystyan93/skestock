using FluentResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Moq;
using NUnit.Framework;
using Shouldly;
using skestock.Application.Common.Behaviours;
using skestock.Application.Common.Caching;
using skestock.Application.Common.Errors;
using skestock.Application.Features.FruitDessertPlans;
using skestock.Application.Features.FruitDessertPlans.Abstractions;
using skestock.Application.Features.FruitDessertPlans.Commands.SendFruitDessertMessage;
using skestock.Domain.Entities.Congregations;
using skestock.Domain.Entities.FruitDessert;
using skestock.Domain.Entities.SchoolClasses;
using skestock.Domain.Enums;
using skestock.Infrastructure.FruitDessert;

namespace skestock.Application.UnitTests.Features.FruitDessertPlans.Commands;

public class SendFruitDessertMessageTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 9, 8, 0, 0, TimeSpan.Zero);

    private Mock<IMessageSender> _sender = null!;

    [SetUp]
    public void SetUp()
    {
        _sender = new Mock<IMessageSender>();
        _sender.Setup(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string phone, string body, CancellationToken _) =>
                Result.Ok(new MessageDispatch($"https://wa.me/{phone}", FruitDessertMessageStatus.Opened)));
    }

    private SendFruitDessertMessageCommandHandler CreateHandler(FruitDessertTestDbContext context) =>
        new(context, new MessageTemplateRenderer(), new PhoneNumberNormalizer(), _sender.Object,
            new FakeTimeProvider(Now));

    private sealed record Seed(SchoolClass Class, FruitDessertPlanEntry Entry, Congregation Congregation, MessageTemplate Template);

    private static async Task<Seed> SeedAsync(
        FruitDessertTestDbContext context,
        string? contactPerson = "Maria",
        string? contactPhone = "0722 123 456",
        string templateBody = "Bună {ContactPerson}, {Congregation} la {Date} pentru {ClassName}",
        bool withCongregation = true)
    {
        var schoolClass = new SchoolClass { Name = "Toamna" };
        var congregation = new Congregation
        {
            Name = "Nord", Email = "n@example.com", DessertContactPerson = contactPerson, DessertContactPhone = contactPhone
        };
        var template = new MessageTemplate
        {
            Type = FruitDessertMessageType.Reminder, Name = "R", Body = templateBody, IsActive = true,
            CreatedDate = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)
        };
        context.SchoolClasses.Add(schoolClass);
        context.Congregations.Add(congregation);
        context.MessageTemplates.Add(template);
        var entry = new FruitDessertPlanEntry
        {
            SchoolClassId = schoolClass.Id,
            Date = new DateOnly(2026, 11, 9),
            CongregationId = withCongregation ? congregation.Id : null
        };
        context.FruitDessertPlanEntries.Add(entry);
        await context.SaveChangesAsync();
        return new Seed(schoolClass, entry, congregation, template);
    }

    private static SendFruitDessertMessageCommand CommandFor(Seed seed, FruitDessertMessageType type = FruitDessertMessageType.Reminder) =>
        new() { ClassId = seed.Class.Id, EntryId = seed.Entry.Id, Type = type };

    [Test]
    public void Validator_RejectsEmptyIdsAndUndefinedType()
    {
        var result = new SendFruitDessertMessageCommandValidator()
            .Validate(new SendFruitDessertMessageCommand { Type = (FruitDessertMessageType)99 });

        result.Errors.Select(e => e.PropertyName).ShouldBe(
            [nameof(SendFruitDessertMessageCommand.ClassId), nameof(SendFruitDessertMessageCommand.EntryId),
             nameof(SendFruitDessertMessageCommand.Type)],
            ignoreOrder: true);
    }

    [Test]
    public void Command_RequiresOpenClass_AndInvalidatesPlanTag()
    {
        var command = new SendFruitDessertMessageCommand
        {
            ClassId = Guid.NewGuid(), EntryId = Guid.NewGuid(), Type = FruitDessertMessageType.Thanks
        };

        ((IRequiresOpenClass)command).ClassId.ShouldBe(command.ClassId);
        ((ICacheInvalidation)command).Tags.ShouldBe([CacheConstants.PlanTag(command.ClassId)]);
    }

    [Test]
    public async Task Handle_Success_AppendsSnapshotRow_AndReturnsUrl()
    {
        await using var context = FruitDessertTestDbContext.Create();
        var seed = await SeedAsync(context);

        var result = await CreateHandler(context).Handle(CommandFor(seed), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Url.ShouldBe("https://wa.me/40722123456");
        result.Value.Status.ShouldBe(FruitDessertMessageStatus.Opened);
        var message = await context.FruitDessertMessages.SingleAsync();
        message.EntryId.ShouldBe(seed.Entry.Id);
        message.Type.ShouldBe(FruitDessertMessageType.Reminder);
        message.Status.ShouldBe(FruitDessertMessageStatus.Opened);
        message.SentAt.ShouldBe(Now);
        message.RecipientPhone.ShouldBe("40722123456");
        message.Body.ShouldBe("Bună Maria, Nord la luni, 9 noiembrie 2026 pentru Toamna");
        message.TemplateId.ShouldBe(seed.Template.Id);
        _sender.Verify(s => s.SendAsync("40722123456", message.Body, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Handle_Resend_AppendsASecondRow()
    {
        await using var context = FruitDessertTestDbContext.Create();
        var seed = await SeedAsync(context);
        var handler = CreateHandler(context);

        await handler.Handle(CommandFor(seed), CancellationToken.None);
        await handler.Handle(CommandFor(seed), CancellationToken.None);

        (await context.FruitDessertMessages.CountAsync()).ShouldBe(2);
    }

    [Test]
    public async Task Handle_SoftDeletedCongregation_StillSends()
    {
        await using var context = FruitDessertTestDbContext.Create();
        var seed = await SeedAsync(context);
        seed.Congregation.DeletedDate = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync();

        var result = await CreateHandler(context).Handle(CommandFor(seed), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
    }

    [Test]
    public async Task Handle_NoCongregation_ReturnsContactMissing()
    {
        await using var context = FruitDessertTestDbContext.Create();
        var seed = await SeedAsync(context, withCongregation: false);

        var result = await CreateHandler(context).Handle(CommandFor(seed), CancellationToken.None);

        result.Errors.ShouldContain(e => e is FruitDessertErrors.ContactMissing);
        (await context.FruitDessertMessages.CountAsync()).ShouldBe(0);
    }

    [TestCase(null, "0722123456")]
    [TestCase("  ", "0722123456")]
    [TestCase("Maria", null)]
    [TestCase("Maria", "")]
    public async Task Handle_MissingContactPersonOrPhone_ReturnsContactMissing(string? person, string? phone)
    {
        await using var context = FruitDessertTestDbContext.Create();
        var seed = await SeedAsync(context, person, phone);

        var result = await CreateHandler(context).Handle(CommandFor(seed), CancellationToken.None);

        result.Errors.ShouldContain(e => e is FruitDessertErrors.ContactMissing);
    }

    [Test]
    public async Task Handle_InvalidPhone_ReturnsInvalidPhone()
    {
        await using var context = FruitDessertTestDbContext.Create();
        var seed = await SeedAsync(context, contactPhone: "nu-e-telefon");

        var result = await CreateHandler(context).Handle(CommandFor(seed), CancellationToken.None);

        result.Errors.ShouldContain(e => e is FruitDessertErrors.InvalidPhone);
        (await context.FruitDessertMessages.CountAsync()).ShouldBe(0);
    }

    [Test]
    public async Task Handle_NoActiveTemplateForType_ReturnsActiveTemplateMissing()
    {
        await using var context = FruitDessertTestDbContext.Create();
        var seed = await SeedAsync(context);

        var result = await CreateHandler(context)
            .Handle(CommandFor(seed, FruitDessertMessageType.Thanks), CancellationToken.None);

        result.Errors.ShouldContain(e => e is FruitDessertErrors.ActiveTemplateMissing);
    }

    [Test]
    public async Task Handle_TemplateWithUnknownPlaceholder_FailsAndSavesNothing()
    {
        await using var context = FruitDessertTestDbContext.Create();
        var seed = await SeedAsync(context, templateBody: "Salut {Foo}");

        var result = await CreateHandler(context).Handle(CommandFor(seed), CancellationToken.None);

        result.Errors.ShouldContain(e => e is FruitDessertErrors.UnknownPlaceholder);
        (await context.FruitDessertMessages.CountAsync()).ShouldBe(0);
        _sender.VerifyNoOtherCalls();
    }

    [Test]
    public async Task Handle_UsesTheNewestActiveTemplate()
    {
        await using var context = FruitDessertTestDbContext.Create();
        var seed = await SeedAsync(context);
        seed.Template.IsActive = false;
        var newer = new MessageTemplate
        {
            Type = FruitDessertMessageType.Reminder, Name = "R2", Body = "Nou {ContactPerson}", IsActive = true,
            CreatedDate = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero)
        };
        context.MessageTemplates.Add(newer);
        await context.SaveChangesAsync();

        await CreateHandler(context).Handle(CommandFor(seed), CancellationToken.None);

        var message = await context.FruitDessertMessages.SingleAsync();
        message.TemplateId.ShouldBe(newer.Id);
        message.Body.ShouldBe("Nou Maria");
    }

    [Test]
    public async Task Handle_EntryOfAnotherClass_ReturnsEntryNotFound()
    {
        await using var context = FruitDessertTestDbContext.Create();
        var seed = await SeedAsync(context);

        var result = await CreateHandler(context).Handle(
            new SendFruitDessertMessageCommand
            {
                ClassId = Guid.NewGuid(), EntryId = seed.Entry.Id, Type = FruitDessertMessageType.Reminder
            }, CancellationToken.None);

        result.Errors.ShouldContain(e => e is FruitDessertErrors.EntryNotFound);
    }
}

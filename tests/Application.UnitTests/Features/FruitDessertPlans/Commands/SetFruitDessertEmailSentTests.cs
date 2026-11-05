using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;
using skestock.Application.Common.Behaviours;
using skestock.Application.Common.Caching;
using skestock.Application.Common.Errors;
using skestock.Application.Features.FruitDessertPlans;
using skestock.Application.Features.FruitDessertPlans.Commands.SetFruitDessertEmailSent;
using skestock.Domain.Entities.FruitDessert;
using skestock.Domain.Entities.SchoolClasses;
using skestock.Infrastructure.FruitDessert;

namespace skestock.Application.UnitTests.Features.FruitDessertPlans.Commands;

public class SetFruitDessertEmailSentTests
{
    private static SetFruitDessertEmailSentCommandHandler CreateHandler(FruitDessertTestDbContext context) =>
        new(context, new PhoneNumberNormalizer());

    private static async Task<(SchoolClass Class, FruitDessertPlanEntry Entry)> SeedAsync(
        FruitDessertTestDbContext context)
    {
        var schoolClass = new SchoolClass { Name = "Toamna" };
        context.SchoolClasses.Add(schoolClass);
        var entry = new FruitDessertPlanEntry { SchoolClassId = schoolClass.Id, Date = new DateOnly(2026, 11, 2) };
        context.FruitDessertPlanEntries.Add(entry);
        await context.SaveChangesAsync();
        return (schoolClass, entry);
    }

    [Test]
    public void Validator_EmptyIds_Fail()
    {
        var result = new SetFruitDessertEmailSentCommandValidator().Validate(new SetFruitDessertEmailSentCommand());

        result.Errors.Select(e => e.PropertyName).ShouldBe(
            [nameof(SetFruitDessertEmailSentCommand.ClassId), nameof(SetFruitDessertEmailSentCommand.EntryId)],
            ignoreOrder: true);
    }

    [Test]
    public void Command_RequiresOpenClass_AndInvalidatesPlanTag()
    {
        var command = new SetFruitDessertEmailSentCommand { ClassId = Guid.NewGuid(), EntryId = Guid.NewGuid() };

        ((IRequiresOpenClass)command).ClassId.ShouldBe(command.ClassId);
        ((ICacheInvalidation)command).Tags.ShouldBe([CacheConstants.PlanTag(command.ClassId)]);
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task Handle_PersistsTheFlag(bool emailSent)
    {
        await using var context = FruitDessertTestDbContext.Create();
        var (schoolClass, entry) = await SeedAsync(context);
        entry.EmailSent = !emailSent;
        await context.SaveChangesAsync();

        var result = await CreateHandler(context).Handle(new SetFruitDessertEmailSentCommand
        {
            ClassId = schoolClass.Id, EntryId = entry.Id, EmailSent = emailSent
        }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.EmailSent.ShouldBe(emailSent);
        (await context.FruitDessertPlanEntries.SingleAsync()).EmailSent.ShouldBe(emailSent);
    }

    [Test]
    public async Task Handle_UnknownEntry_ReturnsEntryNotFound()
    {
        await using var context = FruitDessertTestDbContext.Create();
        var (schoolClass, _) = await SeedAsync(context);

        var result = await CreateHandler(context).Handle(new SetFruitDessertEmailSentCommand
        {
            ClassId = schoolClass.Id, EntryId = Guid.NewGuid(), EmailSent = true
        }, CancellationToken.None);

        result.Errors.ShouldContain(e => e is FruitDessertErrors.EntryNotFound);
    }

    [Test]
    public async Task Handle_EntryOfAnotherClass_ReturnsEntryNotFound()
    {
        await using var context = FruitDessertTestDbContext.Create();
        var (_, entry) = await SeedAsync(context);

        var result = await CreateHandler(context).Handle(new SetFruitDessertEmailSentCommand
        {
            ClassId = Guid.NewGuid(), EntryId = entry.Id, EmailSent = true
        }, CancellationToken.None);

        result.Errors.ShouldContain(e => e is FruitDessertErrors.EntryNotFound);
        (await context.FruitDessertPlanEntries.SingleAsync()).EmailSent.ShouldBeFalse();
    }
}

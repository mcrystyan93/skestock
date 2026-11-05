using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;
using skestock.Application.Common.Behaviours;
using skestock.Application.Common.Caching;
using skestock.Application.Common.Errors;
using skestock.Application.Features.FruitDessertPlans;
using skestock.Application.Features.FruitDessertPlans.Commands.SetFruitDessertCongregation;
using skestock.Domain.Entities.Congregations;
using skestock.Domain.Entities.FruitDessert;
using skestock.Domain.Entities.SchoolClasses;
using skestock.Infrastructure.FruitDessert;

namespace skestock.Application.UnitTests.Features.FruitDessertPlans.Commands;

public class SetFruitDessertCongregationTests
{
    private static SetFruitDessertCongregationCommandHandler CreateHandler(FruitDessertTestDbContext context) =>
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
        var result = new SetFruitDessertCongregationCommandValidator().Validate(new SetFruitDessertCongregationCommand());

        result.Errors.Select(e => e.PropertyName).ShouldBe(
            [nameof(SetFruitDessertCongregationCommand.ClassId), nameof(SetFruitDessertCongregationCommand.EntryId)],
            ignoreOrder: true);
    }

    [Test]
    public void Command_RequiresOpenClass_AndInvalidatesPlanTag()
    {
        var command = new SetFruitDessertCongregationCommand { ClassId = Guid.NewGuid(), EntryId = Guid.NewGuid() };

        ((IRequiresOpenClass)command).ClassId.ShouldBe(command.ClassId);
        ((ICacheInvalidation)command).Tags.ShouldBe([CacheConstants.PlanTag(command.ClassId)]);
    }

    [Test]
    public async Task Handle_SetsCongregation_AndReturnsContactDetails()
    {
        await using var context = FruitDessertTestDbContext.Create();
        var (schoolClass, entry) = await SeedAsync(context);
        var congregation = new Congregation
        {
            Name = "Nord", Email = "n@example.com", DessertContactPerson = "Maria", DessertContactPhone = "0722123456"
        };
        context.Congregations.Add(congregation);
        await context.SaveChangesAsync();

        var result = await CreateHandler(context).Handle(new SetFruitDessertCongregationCommand
        {
            ClassId = schoolClass.Id, EntryId = entry.Id, CongregationId = congregation.Id
        }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.CongregationName.ShouldBe("Nord");
        result.Value.ContactPerson.ShouldBe("Maria");
        result.Value.NormalizedPhone.ShouldBe("40722123456");
        (await context.FruitDessertPlanEntries.SingleAsync()).CongregationId.ShouldBe(congregation.Id);
    }

    [Test]
    public async Task Handle_NullCongregation_ClearsIt()
    {
        await using var context = FruitDessertTestDbContext.Create();
        var (schoolClass, entry) = await SeedAsync(context);
        var congregation = new Congregation { Name = "Nord", Email = "n@example.com" };
        context.Congregations.Add(congregation);
        entry.CongregationId = congregation.Id;
        await context.SaveChangesAsync();

        var result = await CreateHandler(context).Handle(new SetFruitDessertCongregationCommand
        {
            ClassId = schoolClass.Id, EntryId = entry.Id, CongregationId = null
        }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.CongregationId.ShouldBeNull();
        (await context.FruitDessertPlanEntries.SingleAsync()).CongregationId.ShouldBeNull();
    }

    [Test]
    public async Task Handle_UnknownEntry_ReturnsEntryNotFound()
    {
        await using var context = FruitDessertTestDbContext.Create();
        var (schoolClass, _) = await SeedAsync(context);

        var result = await CreateHandler(context).Handle(new SetFruitDessertCongregationCommand
        {
            ClassId = schoolClass.Id, EntryId = Guid.NewGuid()
        }, CancellationToken.None);

        result.Errors.ShouldContain(e => e is FruitDessertErrors.EntryNotFound);
    }

    [Test]
    public async Task Handle_EntryOfAnotherClass_ReturnsEntryNotFound()
    {
        await using var context = FruitDessertTestDbContext.Create();
        var (_, entry) = await SeedAsync(context);

        var result = await CreateHandler(context).Handle(new SetFruitDessertCongregationCommand
        {
            ClassId = Guid.NewGuid(), EntryId = entry.Id
        }, CancellationToken.None);

        result.Errors.ShouldContain(e => e is FruitDessertErrors.EntryNotFound);
    }

    [Test]
    public async Task Handle_UnknownOrDeletedCongregation_ReturnsCongregationNotFound()
    {
        await using var context = FruitDessertTestDbContext.Create();
        var (schoolClass, entry) = await SeedAsync(context);
        var deleted = new Congregation { Name = "Veche", Email = "v@example.com", DeletedDate = DateTimeOffset.UtcNow };
        context.Congregations.Add(deleted);
        await context.SaveChangesAsync();
        var handler = CreateHandler(context);

        foreach (var congregationId in new[] { Guid.NewGuid(), deleted.Id })
        {
            var result = await handler.Handle(new SetFruitDessertCongregationCommand
            {
                ClassId = schoolClass.Id, EntryId = entry.Id, CongregationId = congregationId
            }, CancellationToken.None);

            result.Errors.ShouldContain(e => e is CongregationErrors.CongregationNotFound);
        }

        (await context.FruitDessertPlanEntries.SingleAsync()).CongregationId.ShouldBeNull();
    }
}

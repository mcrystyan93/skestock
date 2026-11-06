using NUnit.Framework;
using Shouldly;
using skestock.Application.Common.Errors;
using skestock.Application.Features.FruitDessertPlans.Queries.GetFruitDessertPlan;
using skestock.Domain.Entities.Congregations;
using skestock.Domain.Entities.FruitDessert;
using skestock.Domain.Entities.SchoolClasses;
using skestock.Domain.Enums;
using skestock.Infrastructure.FruitDessert;

namespace skestock.Application.UnitTests.Features.FruitDessertPlans.Queries;

public class GetFruitDessertPlanHandlerTests
{
    // 2026-11-02 is a Monday: Mon-Fri of two weeks = 10 working days.
    private static readonly DateOnly Start = new(2026, 11, 2);
    private static readonly DateOnly End = new(2026, 11, 13);

    private static GetFruitDessertPlanQueryHandler CreateHandler(FruitDessertTestDbContext context) =>
        new(context, new WorkingDayProvider(), new PhoneNumberNormalizer());

    private static async Task<SchoolClass> AddClassAsync(
        FruitDessertTestDbContext context, ClassStatus status = ClassStatus.Active)
    {
        var schoolClass = new SchoolClass { Name = "Toamna", StartDate = Start, EndDate = End, Status = status };
        context.SchoolClasses.Add(schoolClass);
        await context.SaveChangesAsync();
        return schoolClass;
    }

    [Test]
    public async Task Handle_FirstCall_CreatesExactlyTheWorkingDays_OrderedByDate()
    {
        await using var context = FruitDessertTestDbContext.Create();
        var schoolClass = await AddClassAsync(context);

        var result = await CreateHandler(context)
            .Handle(new GetFruitDessertPlanQuery { ClassId = schoolClass.Id }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Count.ShouldBe(10);
        result.Value.Select(e => e.Date).ShouldBe(result.Value.Select(e => e.Date).Order());
        result.Value.ShouldAllBe(e => e.Date.DayOfWeek != DayOfWeek.Saturday && e.Date.DayOfWeek != DayOfWeek.Sunday);
        context.FruitDessertPlanEntries.Count().ShouldBe(10);
    }

    [Test]
    public async Task Handle_SecondCall_CreatesNoDuplicates()
    {
        await using var context = FruitDessertTestDbContext.Create();
        var schoolClass = await AddClassAsync(context);
        var handler = CreateHandler(context);
        var query = new GetFruitDessertPlanQuery { ClassId = schoolClass.Id };

        await handler.Handle(query, CancellationToken.None);
        var second = await handler.Handle(query, CancellationToken.None);

        second.Value.Count.ShouldBe(10);
        context.FruitDessertPlanEntries.Count().ShouldBe(10);
    }

    [Test]
    public async Task Handle_ExtendedEndDate_AddsOnlyNewDays()
    {
        await using var context = FruitDessertTestDbContext.Create();
        var schoolClass = await AddClassAsync(context);
        var handler = CreateHandler(context);
        var query = new GetFruitDessertPlanQuery { ClassId = schoolClass.Id };
        var first = await handler.Handle(query, CancellationToken.None);
        var existingIds = first.Value.Select(e => e.Id).ToHashSet();

        schoolClass.EndDate = new DateOnly(2026, 11, 20);
        await context.SaveChangesAsync();
        var second = await handler.Handle(query, CancellationToken.None);

        second.Value.Count.ShouldBe(15);
        context.FruitDessertPlanEntries.Count().ShouldBe(15);
        existingIds.ShouldAllBe(id => second.Value.Any(e => e.Id == id));
    }

    [Test]
    public async Task Handle_ShrunkInterval_KeepsRowsButDoesNotListThem()
    {
        await using var context = FruitDessertTestDbContext.Create();
        var schoolClass = await AddClassAsync(context);
        var handler = CreateHandler(context);
        var query = new GetFruitDessertPlanQuery { ClassId = schoolClass.Id };
        await handler.Handle(query, CancellationToken.None);

        schoolClass.EndDate = new DateOnly(2026, 11, 6);
        await context.SaveChangesAsync();
        var result = await handler.Handle(query, CancellationToken.None);

        result.Value.Count.ShouldBe(5);
        context.FruitDessertPlanEntries.Count().ShouldBe(10);
    }

    [Test]
    public async Task Handle_ClosedClass_CreatesNothing_AndReturnsExistingRows()
    {
        await using var context = FruitDessertTestDbContext.Create();
        var schoolClass = await AddClassAsync(context, ClassStatus.Closed);
        var handler = CreateHandler(context);
        var query = new GetFruitDessertPlanQuery { ClassId = schoolClass.Id };

        var empty = await handler.Handle(query, CancellationToken.None);
        empty.Value.ShouldBeEmpty();
        context.FruitDessertPlanEntries.Count().ShouldBe(0);

        context.FruitDessertPlanEntries.Add(new FruitDessertPlanEntry { SchoolClassId = schoolClass.Id, Date = Start });
        await context.SaveChangesAsync();
        var existing = await handler.Handle(query, CancellationToken.None);

        existing.Value.Count.ShouldBe(1);
        context.FruitDessertPlanEntries.Count().ShouldBe(1);
    }

    [Test]
    public async Task Handle_UnknownClass_ReturnsNotFound()
    {
        await using var context = FruitDessertTestDbContext.Create();

        var result = await CreateHandler(context)
            .Handle(new GetFruitDessertPlanQuery { ClassId = Guid.NewGuid() }, CancellationToken.None);

        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(e => e is SchoolClassErrors.SchoolClassNotFound);
    }

    [Test]
    public async Task Handle_DeletedCongregation_StillReturnsNameAndDeletedFlag()
    {
        await using var context = FruitDessertTestDbContext.Create();
        var schoolClass = await AddClassAsync(context);
        var congregation = new Congregation { Name = "Veche", Email = "v@example.com", DeletedDate = DateTimeOffset.UtcNow };
        context.Congregations.Add(congregation);
        context.FruitDessertPlanEntries.Add(new FruitDessertPlanEntry
        {
            SchoolClassId = schoolClass.Id, Date = Start, CongregationId = congregation.Id
        });
        await context.SaveChangesAsync();

        var result = await CreateHandler(context)
            .Handle(new GetFruitDessertPlanQuery { ClassId = schoolClass.Id }, CancellationToken.None);

        var entry = result.Value.Single(e => e.Date == Start);
        entry.CongregationId.ShouldBe(congregation.Id);
        entry.CongregationName.ShouldBe("Veche");
        entry.CongregationDeleted.ShouldBeTrue();
    }

    [Test]
    public async Task Handle_ContactAndPhone_AreLiveFromTheCongregation()
    {
        await using var context = FruitDessertTestDbContext.Create();
        var schoolClass = await AddClassAsync(context);
        var congregation = new Congregation
        {
            Name = "Nord", Email = "n@example.com", DessertContactPerson = "Maria", DessertContactPhone = "0722 123 456"
        };
        context.Congregations.Add(congregation);
        context.FruitDessertPlanEntries.Add(new FruitDessertPlanEntry
        {
            SchoolClassId = schoolClass.Id, Date = Start, CongregationId = congregation.Id
        });
        await context.SaveChangesAsync();
        var handler = CreateHandler(context);
        var query = new GetFruitDessertPlanQuery { ClassId = schoolClass.Id };

        var before = (await handler.Handle(query, CancellationToken.None)).Value.Single(e => e.Date == Start);
        before.ContactPerson.ShouldBe("Maria");
        before.ContactPhone.ShouldBe("0722 123 456");
        before.NormalizedPhone.ShouldBe("40722123456");

        congregation.DessertContactPhone = "nu-e-telefon";
        congregation.DessertContactPerson = "Ana";
        await context.SaveChangesAsync();
        var after = (await handler.Handle(query, CancellationToken.None)).Value.Single(e => e.Date == Start);

        after.ContactPerson.ShouldBe("Ana");
        after.NormalizedPhone.ShouldBeNull();
    }

    [Test]
    public async Task Handle_SentFlags_FollowMessageRows()
    {
        await using var context = FruitDessertTestDbContext.Create();
        var schoolClass = await AddClassAsync(context);
        var template = new MessageTemplate
        {
            Type = FruitDessertMessageType.Reminder, Name = "R", Body = "b", IsActive = true
        };
        var entry = new FruitDessertPlanEntry { SchoolClassId = schoolClass.Id, Date = Start };
        context.MessageTemplates.Add(template);
        context.FruitDessertPlanEntries.Add(entry);
        await context.SaveChangesAsync();
        context.FruitDessertMessages.Add(new FruitDessertMessage
        {
            EntryId = entry.Id, Type = FruitDessertMessageType.Reminder, Status = FruitDessertMessageStatus.Opened,
            SentAt = DateTimeOffset.UtcNow, RecipientPhone = "40722123456", Body = "x", TemplateId = template.Id
        });
        await context.SaveChangesAsync();

        var result = await CreateHandler(context)
            .Handle(new GetFruitDessertPlanQuery { ClassId = schoolClass.Id }, CancellationToken.None);

        var listed = result.Value.Single(e => e.Date == Start);
        listed.ReminderSent.ShouldBeTrue();
        listed.ThanksSent.ShouldBeFalse();
        result.Value.Count(e => e.ReminderSent).ShouldBe(1);
    }
}

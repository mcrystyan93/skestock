using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using skestock.Application.Features.FruitDessertPlans.Queries.GetFruitDessertPlan;
using skestock.Domain.Entities.FruitDessert;
using skestock.Domain.Entities.SchoolClasses;
using skestock.Domain.Enums;

namespace skestock.Application.FunctionalTests.Features.FruitDessertPlans;

public class FruitDessertPlanQueryTests : TestBase
{
    // 2026-11-02 is a Monday: Mon-Fri of two weeks = 10 working days.
    private static async Task<SchoolClass> AddClassAsync(DateOnly end, ClassStatus status = ClassStatus.Active)
    {
        var schoolClass = new SchoolClass
        {
            Name = $"cls-{Guid.NewGuid():N}"[..20],
            StartDate = new DateOnly(2026, 11, 2),
            EndDate = end,
            Status = status
        };
        await TestApp.AddAsync(schoolClass);
        return schoolClass;
    }

    private static Task<int> CountEntriesAsync(Guid classId) => ExecuteAsync(
        context => context.FruitDessertPlanEntries.CountAsync(e => e.SchoolClassId == classId));

    internal static async Task<T> ExecuteAsync<T>(Func<skestock.Infrastructure.Data.ApplicationDbContext, Task<T>> action)
    {
        using var scope = FunctionalTestSetup.ScopeFactory.CreateScope();
        return await action(scope.ServiceProvider.GetRequiredService<skestock.Infrastructure.Data.ApplicationDbContext>());
    }

    [Test]
    public async Task List_CreatesRows_AndSecondListCreatesNoDuplicates()
    {
        var schoolClass = await AddClassAsync(new DateOnly(2026, 11, 13));

        var first = await TestApp.SendAsync(new GetFruitDessertPlanQuery { ClassId = schoolClass.Id });
        var second = await TestApp.SendAsync(new GetFruitDessertPlanQuery { ClassId = schoolClass.Id });

        first.Value.Count.ShouldBe(10);
        second.Value.Count.ShouldBe(10);
        (await CountEntriesAsync(schoolClass.Id)).ShouldBe(10);
    }

    [Test]
    public async Task List_AfterExtendingTheInterval_AddsOnlyNewDays()
    {
        var schoolClass = await AddClassAsync(new DateOnly(2026, 11, 13));
        var first = await TestApp.SendAsync(new GetFruitDessertPlanQuery { ClassId = schoolClass.Id });
        await ExecuteAsync(async context =>
        {
            await context.SchoolClasses.Where(c => c.Id == schoolClass.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.EndDate, new DateOnly(2026, 11, 20)));
            return 0;
        });

        var second = await TestApp.SendAsync(new GetFruitDessertPlanQuery { ClassId = schoolClass.Id });

        second.Value.Count.ShouldBe(15);
        first.Value.Select(e => e.Id).ShouldBeSubsetOf(second.Value.Select(e => e.Id));
        (await CountEntriesAsync(schoolClass.Id)).ShouldBe(15);
    }

    [Test]
    public async Task ConcurrentFirstLists_DoNotThrow_AndLeaveOneRowPerDay()
    {
        var schoolClass = await AddClassAsync(new DateOnly(2026, 11, 13));
        var query = new GetFruitDessertPlanQuery { ClassId = schoolClass.Id };

        var results = await Task.WhenAll(
            TestApp.SendAsync(query), TestApp.SendAsync(query), TestApp.SendAsync(query));

        results.ShouldAllBe(r => r.IsSuccess);
        (await CountEntriesAsync(schoolClass.Id)).ShouldBe(10);
        var dates = await ExecuteAsync(context => context.FruitDessertPlanEntries
            .Where(e => e.SchoolClassId == schoolClass.Id).Select(e => e.Date).ToListAsync());
        dates.Distinct().Count().ShouldBe(10);
    }

    [Test]
    public async Task UniqueIndex_RejectsDuplicateClassAndDate()
    {
        var schoolClass = await AddClassAsync(new DateOnly(2026, 11, 13));
        var date = new DateOnly(2026, 11, 2);
        await TestApp.AddAsync(new FruitDessertPlanEntry { SchoolClassId = schoolClass.Id, Date = date });

        var act = async () => await TestApp.AddAsync(new FruitDessertPlanEntry { SchoolClassId = schoolClass.Id, Date = date });

        await act.ShouldThrowAsync<DbUpdateException>();
    }

    [Test]
    public async Task MessageTemplates_UniqueIndexRejectsASecondActiveTemplateOfTheSameType()
    {
        var act = async () => await TestApp.AddAsync(new MessageTemplate
        {
            Type = FruitDessertMessageType.Reminder, Name = "Duplicat", Body = "x", IsActive = true
        });

        await act.ShouldThrowAsync<DbUpdateException>();
    }

    [Test]
    public async Task MessageTemplates_AreSeededWithOneActivePerType()
    {
        var templates = await ExecuteAsync(context => context.MessageTemplates.Where(t => t.IsActive).ToListAsync());

        templates.Select(t => t.Type).ShouldBe(
            [FruitDessertMessageType.Reminder, FruitDessertMessageType.Thanks], ignoreOrder: true);
    }
}

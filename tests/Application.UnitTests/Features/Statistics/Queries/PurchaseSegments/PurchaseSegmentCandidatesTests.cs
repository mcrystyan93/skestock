using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;
using skestock.Application.Features.Statistics.Queries.PurchaseSegments;
using skestock.Application.UnitTests.Features.GoodsReceipts.Commands.CreateGoodsReceipt;
using skestock.Domain.Entities;
using skestock.Domain.Entities.Categories;
using skestock.Domain.Entities.Items;
using skestock.Domain.Entities.SchoolClasses;
using skestock.Domain.Entities.Statistics;
using skestock.Domain.Enums;

namespace skestock.Application.UnitTests.Features.Statistics.Queries.PurchaseSegments;

[TestFixture]
public sealed class PurchaseSegmentCandidatesTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);

    private static GoodsReceiptTestDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<GoodsReceiptTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static Item NewItem(string name, bool active = true) =>
        new() { Name = name, Unit = "kg", Category = new Category { Name = "Cat" }, IsActive = active };

    private static ItemPurchaseStatistic Stat(
        Item item, PurchaseStatisticsScope scope, decimal quantity, int count, decimal? interval = null,
        SchoolClass? schoolClass = null) =>
        new()
        {
            Scope = scope, Item = item, Class = schoolClass, AverageQuantity = quantity, PurchaseCount = count,
            AverageIntervalDays = interval, ComputedAt = Now, LastPurchasedAt = Now
        };

    private static SchoolClass NewClass(string name) =>
        new() { Name = name, StartDate = new DateOnly(2026, 1, 1), EndDate = new DateOnly(2026, 2, 1) };

    [Test]
    public async Task Inactive_items_are_excluded()
    {
        await using var context = CreateContext();
        context.AddRange(
            Stat(NewItem("On"), PurchaseStatisticsScope.Last365Days, 1m, 1),
            Stat(NewItem("Off", active: false), PurchaseStatisticsScope.Last365Days, 1m, 1));
        await context.SaveChangesAsync();

        var candidates = await PurchaseSegmentCandidates.LoadAsync(context, CancellationToken.None);

        candidates.Select(c => c.Item.Name).ShouldBe(["On"]);
    }

    [Test]
    public async Task Year_row_supplies_the_figures_and_class_rows_the_counts()
    {
        await using var context = CreateContext();
        var item = NewItem("A");
        context.AddRange(
            Stat(item, PurchaseStatisticsScope.Last365Days, 3m, 2, interval: 5m),
            Stat(item, PurchaseStatisticsScope.Class, 1m, 1, schoolClass: NewClass("C0")),
            Stat(item, PurchaseStatisticsScope.Class, 1m, 1, schoolClass: NewClass("C1")));
        await context.SaveChangesAsync();

        var candidate = (await PurchaseSegmentCandidates.LoadAsync(context, CancellationToken.None))
            .ShouldHaveSingleItem();

        candidate.Item.AverageQuantity.ShouldBe(3m);
        candidate.Item.PurchaseCount.ShouldBe(2);
        candidate.Item.AverageIntervalDays.ShouldBe(5m);
        candidate.LastPurchasedAt.ShouldBe(Now);
        candidate.ClassPurchaseCounts.ShouldBe([1, 1]);
    }

    [Test]
    public async Task Without_year_row_figures_average_the_class_rows()
    {
        await using var context = CreateContext();
        var item = NewItem("A");
        context.AddRange(
            Stat(item, PurchaseStatisticsScope.Class, 2m, 1, schoolClass: NewClass("C0")),
            Stat(item, PurchaseStatisticsScope.Class, 5m, 1, schoolClass: NewClass("C1")));
        await context.SaveChangesAsync();

        var candidate = (await PurchaseSegmentCandidates.LoadAsync(context, CancellationToken.None))
            .ShouldHaveSingleItem();

        candidate.Item.AverageQuantity.ShouldBe(3.5m);
        candidate.Item.PurchaseCount.ShouldBe(1);
        candidate.Item.AverageIntervalDays.ShouldBeNull();
        candidate.LastPurchasedAt.ShouldBeNull();
    }
}

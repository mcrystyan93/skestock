using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using NUnit.Framework;
using Shouldly;
using skestock.Application.Features.Statistics;
using skestock.Application.Features.Statistics.Models;
using skestock.Application.Features.Statistics.Queries.GetPurchaseSegments;
using skestock.Application.UnitTests.Features.GoodsReceipts.Commands.CreateGoodsReceipt;
using skestock.Domain.Entities;
using skestock.Domain.Entities.Categories;
using skestock.Domain.Entities.Items;
using skestock.Domain.Entities.SchoolClasses;
using skestock.Domain.Entities.Statistics;
using skestock.Domain.Enums;

namespace skestock.Application.UnitTests.Features.Statistics.Queries.GetPurchaseSegments;

[TestFixture]
public sealed class GetPurchaseSegmentsQueryHandlerTests
{
    private static GoodsReceiptTestDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<GoodsReceiptTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static readonly FakeTimeProvider Clock = new(new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero));

    private static readonly Category Category = new() { Name = "Cat" };

    private static Item NewItem(string name, bool active = true) =>
        new() { Name = name, Unit = "kg", Category = Category, IsActive = active };

    private static ItemPurchaseStatistic Year(Item item, decimal? interval, decimal quantity = 2m, int count = 5, int daysSinceLast = 1) =>
        new()
        {
            Scope = PurchaseStatisticsScope.Last365Days, Item = item, AverageIntervalDays = interval,
            AverageQuantity = quantity, PurchaseCount = count, ComputedAt = Clock.GetUtcNow(),
            LastPurchasedAt = Clock.GetUtcNow().AddDays(-daysSinceLast)
        };

    private static ItemPurchaseStatistic InClass(Item item, SchoolClass schoolClass, int count, decimal quantity = 1m) =>
        new()
        {
            Scope = PurchaseStatisticsScope.Class, Item = item, Class = schoolClass,
            AverageQuantity = quantity, PurchaseCount = count, ComputedAt = Clock.GetUtcNow()
        };

    private static SchoolClass[] Classes(int n) =>
        Enumerable.Range(0, n).Select(i => new SchoolClass
        {
            Name = $"C{i}", StartDate = new DateOnly(2026, 1, 1), EndDate = new DateOnly(2026, 2, 1)
        }).ToArray();

    private static async Task<PurchaseSegmentsDto> RunAsync(GoodsReceiptTestDbContext context)
    {
        await context.SaveChangesAsync(CancellationToken.None);
        var result = await new GetPurchaseSegmentsQueryHandler(context, Clock)
            .Handle(new GetPurchaseSegmentsQuery(), CancellationToken.None);
        result.IsSuccess.ShouldBeTrue();
        return result.Value;
    }

    [TestCase(0.01, "Weekly")]
    [TestCase(10, "Weekly")]
    [TestCase(10.01, "EveryTwoWeeks")]
    [TestCase(21, "EveryTwoWeeks")]
    [TestCase(21.01, "Monthly")]
    [TestCase(45, "Monthly")]
    [TestCase(45.01, "None")]
    public async Task Cadence_boundaries(double days, string expected)
    {
        await using var context = CreateContext();
        var item = NewItem("A");
        context.Add(Year(item, (decimal)days, daysSinceLast: 0));

        var dto = await RunAsync(context);

        var lists = new Dictionary<string, List<PurchaseSegmentItemDto>>
        {
            ["Weekly"] = dto.Weekly, ["EveryTwoWeeks"] = dto.EveryTwoWeeks, ["Monthly"] = dto.Monthly
        };
        foreach (var (name, list) in lists)
            list.Count.ShouldBe(name == expected ? 1 : 0, name);
        dto.OncePerClass.ShouldBeEmpty();
    }

    [Test]
    public async Task Dormant_item_and_zero_interval_item_are_excluded_but_item_within_tolerance_is_kept()
    {
        await using var context = CreateContext();
        context.AddRange(
            Year(NewItem("Dormant"), 5m, daysSinceLast: 11),
            Year(NewItem("SameDay"), 0m),
            Year(NewItem("Tolerated"), 5m, daysSinceLast: 10));

        var dto = await RunAsync(context);

        dto.Weekly.Select(i => i.Name).ShouldBe(["Tolerated"]);
        (dto.EveryTwoWeeks.Count + dto.Monthly.Count + dto.OncePerClass.Count).ShouldBe(0);
    }

    [TestCase(14, 28, "EveryTwoWeeks", true)]
    [TestCase(14, 29, "EveryTwoWeeks", false)]
    [TestCase(30, 60, "Monthly", true)]
    [TestCase(30, 61, "Monthly", false)]
    [TestCase(21, 42, "EveryTwoWeeks", true)]
    [TestCase(45, 90, "Monthly", true)]
    public async Task Dormancy_cutoff_is_twice_the_interval(int interval, int daysSinceLast, string segment, bool kept)
    {
        await using var context = CreateContext();
        context.Add(Year(NewItem("A"), interval, daysSinceLast: daysSinceLast));

        var dto = await RunAsync(context);

        var list = segment == "Monthly" ? dto.Monthly : dto.EveryTwoWeeks;
        list.Count.ShouldBe(kept ? 1 : 0);
        (dto.Weekly.Count + dto.OncePerClass.Count + (segment == "Monthly" ? dto.EveryTwoWeeks : dto.Monthly).Count).ShouldBe(0);
    }

    [Test]
    public async Task Dormant_item_that_qualifies_for_once_per_class_is_still_listed()
    {
        await using var context = CreateContext();
        var item = NewItem("A");
        var classes = Classes(2);
        context.AddRange(Year(item, 5m, daysSinceLast: 400), InClass(item, classes[0], 1), InClass(item, classes[1], 1));

        var dto = await RunAsync(context);

        dto.OncePerClass.Select(i => i.Name).ShouldBe(["A"]);
        dto.Weekly.ShouldBeEmpty();
    }

    [TestCase(-5)]
    [TestCase(0)]
    public async Task Non_positive_interval_lands_in_no_segment(int interval)
    {
        await using var context = CreateContext();
        context.Add(Year(NewItem("A"), interval, daysSinceLast: 0));

        var dto = await RunAsync(context);

        (dto.Weekly.Count + dto.EveryTwoWeeks.Count + dto.Monthly.Count + dto.OncePerClass.Count).ShouldBe(0);
    }

    [Test]
    public async Task Null_interval_appears_nowhere()
    {
        await using var context = CreateContext();
        context.Add(Year(NewItem("A"), null));

        var dto = await RunAsync(context);

        (dto.Weekly.Count + dto.EveryTwoWeeks.Count + dto.Monthly.Count + dto.OncePerClass.Count).ShouldBe(0);
    }

    [Test]
    public async Task Per_class_takes_precedence_over_cadence()
    {
        await using var context = CreateContext();
        var item = NewItem("A");
        var classes = Classes(2);
        context.AddRange(Year(item, 5m, quantity: 3m, count: 2), InClass(item, classes[0], 1), InClass(item, classes[1], 1));

        var dto = await RunAsync(context);

        dto.Weekly.ShouldBeEmpty();
        var single = dto.OncePerClass.ShouldHaveSingleItem();
        single.AverageQuantity.ShouldBe(3m);
        single.PurchaseCount.ShouldBe(2);
    }

    [Test]
    public async Task Once_per_class_without_year_row_averages_class_rows()
    {
        await using var context = CreateContext();
        var item = NewItem("A");
        var classes = Classes(2);
        context.AddRange(InClass(item, classes[0], 1, quantity: 2m), InClass(item, classes[1], 1, quantity: 5m));

        var dto = await RunAsync(context);

        var single = dto.OncePerClass.ShouldHaveSingleItem();
        single.AverageQuantity.ShouldBe(3.5m);
        single.PurchaseCount.ShouldBe(1);
        single.AverageIntervalDays.ShouldBeNull();
    }

    [Test]
    public async Task Eighty_percent_rule_four_of_five_in_three_of_five_out()
    {
        await using var context = CreateContext();
        var four = NewItem("Four");
        var three = NewItem("Three");
        var classes = Classes(5);
        for (var i = 0; i < 5; i++)
        {
            context.Add(InClass(four, classes[i], i < 4 ? 1 : 3));
            context.Add(InClass(three, classes[i], i < 3 ? 1 : 3));
        }

        var dto = await RunAsync(context);

        dto.OncePerClass.Select(i => i.Name).ShouldBe(["Four"]);
    }

    [Test]
    public async Task Single_class_is_not_once_per_class()
    {
        await using var context = CreateContext();
        context.Add(InClass(NewItem("A"), Classes(1)[0], 1));

        (await RunAsync(context)).OncePerClass.ShouldBeEmpty();
    }

    [Test]
    public async Task Inactive_items_are_excluded_and_lists_sorted_by_name()
    {
        await using var context = CreateContext();
        context.AddRange(Year(NewItem("Zahar"), 5m), Year(NewItem("Apa"), 5m), Year(NewItem("Off", active: false), 5m));

        var dto = await RunAsync(context);

        dto.Weekly.Select(i => i.Name).ShouldBe(["Apa", "Zahar"]);
    }

    [Test]
    public void Query_is_cached_under_the_purchase_statistics_tag()
    {
        var query = new GetPurchaseSegmentsQuery();

        query.Tags.ShouldBe([CacheConstants.PurchaseStatisticsTag]);
        query.BuildCacheKey().ShouldBe($"{CacheConstants.PurchaseStatisticsTag}:segments");
    }
}

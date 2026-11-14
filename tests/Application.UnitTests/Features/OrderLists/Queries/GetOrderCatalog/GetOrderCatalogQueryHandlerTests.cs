using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using NUnit.Framework;
using Shouldly;
using skestock.Application.Common.Errors;
using skestock.Application.Features.OrderLists.Models;
using skestock.Application.Features.OrderLists.Queries.GetOrderCatalog;
using skestock.Application.UnitTests.Features.GoodsReceipts.Commands.CreateGoodsReceipt;
using skestock.Domain.Entities;
using skestock.Domain.Entities.Categories;
using skestock.Domain.Entities.Items;
using skestock.Domain.Entities.Locations;
using skestock.Domain.Entities.SchoolClasses;
using skestock.Domain.Entities.Stock;
using skestock.Domain.Entities.Statistics;
using skestock.Domain.Enums;
using ItemsCache = skestock.Application.Features.Items.CacheConstants;
using StatisticsCache = skestock.Application.Features.Statistics.CacheConstants;
using StockCache = skestock.Application.Features.Stock.CacheConstants;

namespace skestock.Application.UnitTests.Features.OrderLists.Queries.GetOrderCatalog;

[TestFixture]
public sealed class GetOrderCatalogQueryHandlerTests
{
    private static readonly FakeTimeProvider Clock = new(new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero));

    private static GoodsReceiptTestDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<GoodsReceiptTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static Item NewItem(string name, Location? location = null, bool active = true) =>
        new()
        {
            Name = name,
            Unit = "kg",
            Category = new Category { Name = "Cat" },
            IsActive = active,
            Location = location
        };

    private static Location NewLocation(string name) => new() { Name = name, Type = "Room" };

    private static SchoolClass NewClass() =>
        new() { Name = "C", StartDate = new DateOnly(2026, 1, 1), EndDate = new DateOnly(2026, 12, 1) };

    private static StockBatch Batch(Item item, Location location, SchoolClass schoolClass, int quantity) =>
        new()
        {
            Item = item,
            Location = location,
            ReceivedClass = schoolClass,
            Quantity = quantity,
            ReceivedDate = new DateOnly(2026, 5, 1)
        };

    private static ClassItemStockVisibility Hidden(SchoolClass schoolClass, Item item, Location location) =>
        new() { ClassId = schoolClass.Id, ItemId = item.Id, LocationId = location.Id, HideWhenZeroStock = true };

    private static ItemPurchaseStatistic Year(
        Item item, decimal? interval, int? lastQuantity = null, int daysSinceLast = 1) =>
        new()
        {
            Scope = PurchaseStatisticsScope.Last365Days,
            Item = item,
            AverageIntervalDays = interval,
            LastQuantity = lastQuantity,
            ComputedAt = Clock.GetUtcNow(),
            LastPurchasedAt = Clock.GetUtcNow().AddDays(-daysSinceLast)
        };

    private static async Task<List<OrderCatalogItemDto>> RunAsync(GoodsReceiptTestDbContext context,
        SchoolClass schoolClass)
    {
        await context.SaveChangesAsync(CancellationToken.None);
        var result = await new GetOrderCatalogQueryHandler(context, Clock)
            .Handle(new GetOrderCatalogQuery { ClassId = schoolClass.Id }, CancellationToken.None);
        result.IsSuccess.ShouldBeTrue();
        return result.Value;
    }

    [Test]
    public async Task Unknown_class_fails_with_not_found()
    {
        await using var context = CreateContext();

        var result = await new GetOrderCatalogQueryHandler(context, Clock)
            .Handle(new GetOrderCatalogQuery { ClassId = Guid.NewGuid() }, CancellationToken.None);

        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<SchoolClassErrors.SchoolClassNotFound>();
    }

    [Test]
    public async Task Inactive_items_are_excluded_and_result_is_sorted_by_name()
    {
        await using var context = CreateContext();
        var schoolClass = NewClass();
        context.AddRange(schoolClass, NewItem("Zahar"), NewItem("Apa"), NewItem("Off", active: false));

        var catalog = await RunAsync(context, schoolClass);

        catalog.Select(i => i.Name).ShouldBe(["Apa", "Zahar"]);
    }

    [Test]
    public async Task Item_in_two_locations_lists_both_and_is_low_only_below_threshold()
    {
        await using var context = CreateContext();
        var schoolClass = NewClass();
        var item = NewItem("Rice");
        var kitchen = NewLocation("Bucatarie");
        var pantry = NewLocation("Camara");
        context.AddRange(schoolClass, Batch(item, kitchen, schoolClass, 2), Batch(item, pantry, schoolClass, 20));
        context.AddRange(
            new ItemLocationThreshold { ItemId = item.Id, LocationId = kitchen.Id, MinThreshold = 5 },
            new ItemLocationThreshold { ItemId = item.Id, LocationId = pantry.Id, MinThreshold = 5 });

        var dto = (await RunAsync(context, schoolClass)).ShouldHaveSingleItem();

        dto.Locations.Select(l => (l.LocationName, l.IsLowStock)).ShouldBe([("Bucatarie", true), ("Camara", false)]);
    }

    [Test]
    public async Task Quantity_equal_to_the_threshold_is_not_low()
    {
        await using var context = CreateContext();
        var schoolClass = NewClass();
        var item = NewItem("Rice");
        var location = NewLocation("A");
        context.AddRange(schoolClass, Batch(item, location, schoolClass, 5));
        context.Add(new ItemLocationThreshold { ItemId = item.Id, LocationId = location.Id, MinThreshold = 5 });

        (await RunAsync(context, schoolClass)).ShouldHaveSingleItem().Locations.ShouldHaveSingleItem()
            .IsLowStock.ShouldBeFalse();
    }

    [Test]
    public async Task Location_without_threshold_is_never_low()
    {
        await using var context = CreateContext();
        var schoolClass = NewClass();
        var item = NewItem("Rice");
        context.AddRange(schoolClass, Batch(item, NewLocation("A"), schoolClass, 0));

        (await RunAsync(context, schoolClass)).ShouldHaveSingleItem().Locations.ShouldHaveSingleItem()
            .IsLowStock.ShouldBeFalse();
    }

    [Test]
    public async Task Hidden_zero_stock_row_is_dropped_and_falls_back_to_default_location()
    {
        await using var context = CreateContext();
        var schoolClass = NewClass();
        var home = NewLocation("Home");
        var other = NewLocation("Other");
        var item = NewItem("Rice", home);
        context.AddRange(schoolClass, Batch(item, other, schoolClass, 0));
        context.Add(Hidden(schoolClass, item, other));

        var location = (await RunAsync(context, schoolClass)).ShouldHaveSingleItem().Locations.ShouldHaveSingleItem();

        location.LocationName.ShouldBe("Home");
        location.IsLowStock.ShouldBeFalse();
    }

    [Test]
    public async Task Hidden_row_with_stock_is_kept()
    {
        await using var context = CreateContext();
        var schoolClass = NewClass();
        var other = NewLocation("Other");
        var item = NewItem("Rice");
        context.AddRange(schoolClass, Batch(item, other, schoolClass, 3));
        context.Add(Hidden(schoolClass, item, other));

        (await RunAsync(context, schoolClass)).ShouldHaveSingleItem().Locations.ShouldHaveSingleItem()
            .LocationName.ShouldBe("Other");
    }

    [Test]
    public async Task No_stock_with_default_location_uses_it_and_without_one_is_empty()
    {
        await using var context = CreateContext();
        var schoolClass = NewClass();
        context.AddRange(schoolClass, NewItem("A", NewLocation("Home")), NewItem("B"));

        var catalog = await RunAsync(context, schoolClass);

        catalog[0].Locations.ShouldHaveSingleItem().LocationName.ShouldBe("Home");
        catalog[1].Locations.ShouldBeEmpty();
    }

    [Test]
    public async Task Stock_in_another_class_is_ignored()
    {
        await using var context = CreateContext();
        var schoolClass = NewClass();
        var otherClass = NewClass();
        var item = NewItem("Rice");
        context.AddRange(schoolClass, otherClass, Batch(item, NewLocation("Elsewhere"), otherClass, 4));

        (await RunAsync(context, schoolClass)).ShouldHaveSingleItem().Locations.ShouldBeEmpty();
    }

    [TestCase(5, true)]
    [TestCase(14, true)]
    [TestCase(30, false)]
    public async Task Frequent_means_weekly_or_every_two_weeks(int interval, bool frequent)
    {
        await using var context = CreateContext();
        var schoolClass = NewClass();
        context.AddRange(schoolClass, Year(NewItem("A"), interval, daysSinceLast: 0));

        (await RunAsync(context, schoolClass)).ShouldHaveSingleItem().IsFrequent.ShouldBe(frequent);
    }

    [Test]
    public async Task Stats_come_from_the_last_365_days_row_and_are_null_without_one()
    {
        await using var context = CreateContext();
        var schoolClass = NewClass();
        var withStats = NewItem("A");
        context.AddRange(
            schoolClass, withStats, NewItem("B"), Year(withStats, 7.5m, lastQuantity: 12),
            new ItemPurchaseStatistic
            {
                Scope = PurchaseStatisticsScope.Last90Days,
                Item = withStats,
                LastQuantity = 99,
                ComputedAt = Clock.GetUtcNow(),
                LastPurchasedAt = Clock.GetUtcNow()
            });

        var catalog = await RunAsync(context, schoolClass);

        catalog[0].LastQuantity.ShouldBe(12);
        catalog[0].AverageIntervalDays.ShouldBe(7.5m);
        catalog[1].LastQuantity.ShouldBeNull();
        catalog[1].AverageIntervalDays.ShouldBeNull();
        catalog[1].IsFrequent.ShouldBeFalse();
    }

    [Test]
    public void Query_is_cached_per_class_with_item_stock_and_statistics_tags()
    {
        var classId = Guid.NewGuid();
        var query = new GetOrderCatalogQuery { ClassId = classId };

        query.Tags.ShouldBe(
        [
            ItemsCache.ItemListTag,
            StockCache.BuildClassTag(classId),
            StockCache.BuildCoarseTag(),
            StatisticsCache.PurchaseStatisticsTag
        ]);
        query.BuildCacheKey().ShouldBe($"order_list:catalog:class:{classId}");
    }
}

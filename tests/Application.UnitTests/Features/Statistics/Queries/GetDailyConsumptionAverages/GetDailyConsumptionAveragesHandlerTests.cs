using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using NUnit.Framework;
using Shouldly;
using skestock.Application.Features.Statistics.Queries.GetDailyConsumptionAverages;
using skestock.Application.UnitTests.Features.GoodsReceipts.Commands.CreateGoodsReceipt;
using skestock.Domain.Entities;

namespace skestock.Application.UnitTests.Features.Statistics.Queries.GetDailyConsumptionAverages;

public class GetDailyConsumptionAveragesHandlerTests
{
    // 2026-09-26 12:00 local in Bucharest.
    private static readonly FakeTimeProvider Clock =
        new(new DateTimeOffset(2026, 9, 26, 9, 0, 0, TimeSpan.Zero));

    private static readonly DateOnly Today = new(2026, 9, 26);

    [Test]
    public async Task Handle_AveragesCompleteDaysEndingYesterday()
    {
        await using var context = CreateContext();
        var seed = await SeedAsync(context);
        context.DailyItemConsumptions.AddRange(
            Row(seed, seed.Item, seed.Location, Today, 100, 100m),          // today: excluded
            Row(seed, seed.Item, seed.Location, Today.AddDays(-1), 7, 14m),
            Row(seed, seed.Item, seed.Location, Today.AddDays(-7), 7, 7m),
            Row(seed, seed.Item, seed.Location, Today.AddDays(-8), 16, 16m), // 30-day window only
            Row(seed, seed.Item, seed.Location, Today.AddDays(-31), 50, 50m)); // outside both
        await context.SaveChangesAsync(CancellationToken.None);

        var result = await Handle(context, new GetDailyConsumptionAveragesQuery());

        result.IsSuccess.ShouldBeTrue();
        var last7 = result.Value.Last7Days;
        last7.FromDate.ShouldBe(Today.AddDays(-7));
        last7.ToDate.ShouldBe(Today.AddDays(-1));
        last7.TotalQuantity.ShouldBe(14);
        last7.AverageQuantity.ShouldBe(2m);
        last7.AverageValue.ShouldBe(3m);

        var last30 = result.Value.Last30Days;
        last30.FromDate.ShouldBe(Today.AddDays(-30));
        last30.TotalQuantity.ShouldBe(30);
        last30.AverageQuantity.ShouldBe(1m);
    }

    [Test]
    public async Task Handle_AppliesItemLocationAndCategoryFilters()
    {
        await using var context = CreateContext();
        var seed = await SeedAsync(context);
        var yesterday = Today.AddDays(-1);
        context.DailyItemConsumptions.AddRange(
            Row(seed, seed.Item, seed.Location, yesterday, 7, 7m),
            Row(seed, seed.Item, seed.OtherLocation, yesterday, 14, 14m),
            Row(seed, seed.OtherItem, seed.Location, yesterday, 21, 21m));
        await context.SaveChangesAsync(CancellationToken.None);

        (await Handle(context, new GetDailyConsumptionAveragesQuery { ItemId = seed.Item.Id }))
            .Value.Last7Days.TotalQuantity.ShouldBe(21);
        (await Handle(context, new GetDailyConsumptionAveragesQuery { LocationId = seed.Location.Id }))
            .Value.Last7Days.TotalQuantity.ShouldBe(28);
        (await Handle(context, new GetDailyConsumptionAveragesQuery { CategoryId = seed.OtherItem.CategoryId }))
            .Value.Last7Days.TotalQuantity.ShouldBe(21);
        (await Handle(context, new GetDailyConsumptionAveragesQuery
            {
                ItemId = seed.Item.Id,
                LocationId = seed.OtherLocation.Id
            }))
            .Value.Last7Days.AverageQuantity.ShouldBe(2m);
    }

    [Test]
    public async Task Handle_WithoutData_ReturnsZeroAverages()
    {
        await using var context = CreateContext();

        var result = await Handle(context, new GetDailyConsumptionAveragesQuery());

        result.Value.Last7Days.AverageQuantity.ShouldBe(0m);
        result.Value.Last30Days.AverageValue.ShouldBe(0m);
    }

    private static ValueTask<FluentResults.Result<Application.Features.Statistics.Models.DailyConsumptionAveragesDto>>
        Handle(GoodsReceiptTestDbContext context, GetDailyConsumptionAveragesQuery query) =>
        new GetDailyConsumptionAveragesHandler(context, Clock).Handle(query, CancellationToken.None);

    private static GoodsReceiptTestDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<GoodsReceiptTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static async Task<Seed> SeedAsync(GoodsReceiptTestDbContext context)
    {
        var schoolClass = new SchoolClass
        {
            Name = "Fall 2026",
            StartDate = new DateOnly(2026, 9, 1),
            EndDate = new DateOnly(2026, 12, 20)
        };
        var item = new Item { Name = "Pencils", Category = new Category { Name = "Stationery" } };
        var otherItem = new Item { Name = "Milk", Category = new Category { Name = "Food" } };
        var location = new Location { Name = "Kitchen", Type = "Kitchen" };
        var otherLocation = new Location { Name = "Storage", Type = "StorageRoom" };

        context.AddRange(schoolClass, item, otherItem, location, otherLocation);
        await context.SaveChangesAsync(CancellationToken.None);

        return new Seed(schoolClass, item, otherItem, location, otherLocation);
    }

    private static DailyItemConsumption Row(
        Seed seed,
        Item item,
        Location location,
        DateOnly date,
        int quantity,
        decimal value) => new()
    {
        Id = Guid.NewGuid(),
        Date = date,
        ItemId = item.Id,
        ClassId = seed.SchoolClass.Id,
        LocationId = location.Id,
        Quantity = quantity,
        TotalValue = value
    };

    private sealed record Seed(
        SchoolClass SchoolClass,
        Item Item,
        Item OtherItem,
        Location Location,
        Location OtherLocation);
}

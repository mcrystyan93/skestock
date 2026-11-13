using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;
using skestock.Application.Features.Statistics.Queries.GetTopDonations;
using skestock.Application.UnitTests.Features.GoodsReceipts.Commands.CreateGoodsReceipt;
using skestock.Domain.Entities.Categories;
using skestock.Domain.Entities.Items;
using skestock.Domain.Entities.Locations;
using skestock.Domain.Entities.SchoolClasses;
using skestock.Domain.Entities.Stock;
using skestock.Domain.Entities.Users;
using skestock.Domain.Enums;

namespace skestock.Application.UnitTests.Features.Statistics.Queries.GetTopDonations;

[TestFixture]
public sealed class GetTopDonationsQueryHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 26, 12, 0, 0, TimeSpan.Zero);

    private sealed class FixedTime : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private static GoodsReceiptTestDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<GoodsReceiptTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static StockTransaction Tx(
        Item item, Location location, SchoolClass schoolClass, UserProfile user,
        StockTransactionType type, int quantity, DateTimeOffset at) =>
        new()
        {
            Item = item, Location = location, Class = schoolClass, User = user,
            Type = type, QuantityChange = quantity, CreatedAt = at
        };

    [Test]
    public async Task Counts_only_donations_and_ranks_them()
    {
        await using var context = CreateContext();
        var category = new Category { Name = "Alimente" };
        var apples = new Item { Name = "Mere", Unit = "kg", Category = category };
        var pears = new Item { Name = "Pere", Unit = "kg", Category = category };
        var schoolClass = new SchoolClass
        {
            Name = "Class", StartDate = new DateOnly(2026, 1, 1), EndDate = new DateOnly(2026, 12, 31)
        };
        var location = new Location { Name = "Cămară", Type = "StorageRoom" };
        var user = new UserProfile { IdentityId = Guid.NewGuid() };
        var recent = Now.AddDays(-10);
        context.AddRange(
            Tx(apples, location, schoolClass, user, StockTransactionType.Donation, 5, recent),
            Tx(pears, location, schoolClass, user, StockTransactionType.Donation, 3, recent),
            Tx(pears, location, schoolClass, user, StockTransactionType.Donation, 3, recent),
            // Purchases and old donations must not count.
            Tx(apples, location, schoolClass, user, StockTransactionType.Order, 100, recent),
            Tx(apples, location, schoolClass, user, StockTransactionType.Donation, 50, Now.AddDays(-200)));
        await context.SaveChangesAsync(CancellationToken.None);

        var result = await new GetTopDonationsQueryHandler(context, new FixedTime()).Handle(
            new GetTopDonationsQuery { Scope = PurchaseStatisticsScope.Last90Days },
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ByQuantity.Select(s => (s.ItemName, s.TotalQuantity)).ShouldBe([("Pere", 6), ("Mere", 5)]);
        result.Value.ByFrequency[0].PurchaseCount.ShouldBe(2);
    }

    [Test]
    public async Task Class_scope_and_category_filter_narrow_the_ranking()
    {
        await using var context = CreateContext();
        var food = new Category { Name = "Alimente" };
        var paper = new Category { Name = "Papetărie" };
        var apples = new Item { Name = "Mere", Unit = "kg", Category = food };
        var pens = new Item { Name = "Pixuri", Unit = "buc", Category = paper };
        var classA = new SchoolClass
        {
            Name = "A", StartDate = new DateOnly(2026, 1, 1), EndDate = new DateOnly(2026, 12, 31)
        };
        var classB = new SchoolClass
        {
            Name = "B", StartDate = new DateOnly(2026, 1, 1), EndDate = new DateOnly(2026, 12, 31)
        };
        var location = new Location { Name = "Cămară", Type = "StorageRoom" };
        var user = new UserProfile { IdentityId = Guid.NewGuid() };
        var old = Now.AddDays(-300);
        context.AddRange(
            Tx(apples, location, classA, user, StockTransactionType.Donation, 4, old),
            Tx(pens, location, classA, user, StockTransactionType.Donation, 9, old),
            Tx(apples, location, classB, user, StockTransactionType.Donation, 7, old));
        await context.SaveChangesAsync(CancellationToken.None);
        var handler = new GetTopDonationsQueryHandler(context, new FixedTime());

        var byClass = await handler.Handle(
            new GetTopDonationsQuery { Scope = PurchaseStatisticsScope.Class, ClassId = classA.Id },
            CancellationToken.None);
        var byCategory = await handler.Handle(
            new GetTopDonationsQuery { Scope = PurchaseStatisticsScope.Last365Days, CategoryId = food.Id },
            CancellationToken.None);

        // Class scope includes donations older than 365 days; only class A's rows count.
        byClass.Value.ByQuantity.Select(s => (s.ItemName, s.TotalQuantity)).ShouldBe([("Pixuri", 9), ("Mere", 4)]);
        byCategory.Value.ByQuantity.Select(s => (s.ItemName, s.TotalQuantity)).ShouldBe([("Mere", 11)]);
    }

    [Test]
    public async Task Ranks_by_value_using_batch_unit_price()
    {
        await using var context = CreateContext();
        var category = new Category { Name = "Alimente" };
        var cheap = new Item { Name = "Cheap", Unit = "kg", Category = category };
        var pricey = new Item { Name = "Pricey", Unit = "kg", Category = category };
        var schoolClass = new SchoolClass
        {
            Name = "A", StartDate = new DateOnly(2026, 1, 1), EndDate = new DateOnly(2026, 12, 31)
        };
        var location = new Location { Name = "Cămară", Type = "StorageRoom" };
        var user = new UserProfile { IdentityId = Guid.NewGuid() };
        var cheapTx = Tx(cheap, location, schoolClass, user, StockTransactionType.Donation, 10, Now.AddDays(-1));
        cheapTx.Batch = new StockBatch { Item = cheap, Location = location, UnitPrice = 1m };
        var priceyTx = Tx(pricey, location, schoolClass, user, StockTransactionType.Donation, 2, Now.AddDays(-1));
        priceyTx.Batch = new StockBatch { Item = pricey, Location = location, UnitPrice = 20m };
        context.AddRange(cheapTx, priceyTx);
        await context.SaveChangesAsync(CancellationToken.None);

        var result = await new GetTopDonationsQueryHandler(context, new FixedTime()).Handle(
            new GetTopDonationsQuery { Scope = PurchaseStatisticsScope.Last90Days },
            CancellationToken.None);

        result.Value.ByValue.Select(s => (s.ItemName, s.TotalValue)).ShouldBe([("Pricey", 40m), ("Cheap", 10m)]);
    }
}

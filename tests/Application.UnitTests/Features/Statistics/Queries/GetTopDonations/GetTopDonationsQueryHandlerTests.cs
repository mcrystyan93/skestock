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
}

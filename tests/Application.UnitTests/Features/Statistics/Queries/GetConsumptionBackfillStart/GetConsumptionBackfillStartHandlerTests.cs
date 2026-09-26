using Microsoft.EntityFrameworkCore;
using skestock.Application.Features.Statistics.Queries.GetConsumptionBackfillStart;
using skestock.Application.UnitTests.Features.GoodsReceipts.Commands.CreateGoodsReceipt;
using skestock.Domain.Entities;
using skestock.Domain.Enums;
using NUnit.Framework;
using Shouldly;

namespace skestock.Application.UnitTests.Features.Statistics.Queries.GetConsumptionBackfillStart;

public class GetConsumptionBackfillStartHandlerTests
{
    private const string TimeZoneId = "Europe/Bucharest";

    private static GoodsReceiptTestDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<GoodsReceiptTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static async Task SeedTransactionsAsync(
        GoodsReceiptTestDbContext context,
        params (DateTimeOffset CreatedAt, int Change, StockTransactionType Type)[] transactions)
    {
        var schoolClass = new SchoolClass
        {
            Name = "Class", StartDate = new DateOnly(2026, 1, 1), EndDate = new DateOnly(2026, 12, 31)
        };
        var item = new Item { Name = "Item", Category = new Category { Name = "Category" } };
        var location = new Location { Name = "Location", Type = "Storage" };
        var user = new UserProfile { IdentityId = Guid.NewGuid() };
        context.AddRange(schoolClass, item, location, user);
        context.AddRange(transactions.Select(t => new StockTransaction
        {
            Class = schoolClass,
            Item = item,
            Location = location,
            User = user,
            CreatedAt = t.CreatedAt,
            QuantityChange = t.Change,
            Type = t.Type
        }));
        await context.SaveChangesAsync(CancellationToken.None);
    }

    private static async Task<DateOnly?> HandleAsync(GoodsReceiptTestDbContext context)
    {
        var result = await new GetConsumptionBackfillStartHandler(context)
            .Handle(new GetConsumptionBackfillStartQuery { TimeZoneId = TimeZoneId }, CancellationToken.None);
        result.IsSuccess.ShouldBeTrue();
        return result.Value;
    }

    [Test]
    public async Task Returns_null_when_there_is_no_consumption()
    {
        await using var context = CreateContext();
        await SeedTransactionsAsync(context,
            (new DateTimeOffset(2026, 3, 1, 10, 0, 0, TimeSpan.Zero), 10, StockTransactionType.Order),
            (new DateTimeOffset(2026, 3, 2, 10, 0, 0, TimeSpan.Zero), -5, StockTransactionType.Transfer));

        (await HandleAsync(context)).ShouldBeNull();
    }

    [Test]
    public async Task Returns_the_local_date_of_the_earliest_consumption_when_nothing_is_materialized()
    {
        await using var context = CreateContext();
        // 22:30 UTC is already the next day in Bucharest (UTC+2 in March).
        await SeedTransactionsAsync(context,
            (new DateTimeOffset(2026, 3, 1, 22, 30, 0, TimeSpan.Zero), -2, StockTransactionType.Usage),
            (new DateTimeOffset(2026, 4, 1, 10, 0, 0, TimeSpan.Zero), -1, StockTransactionType.Usage));

        (await HandleAsync(context)).ShouldBe(new DateOnly(2026, 3, 2));
    }

    [Test]
    public async Task Returns_null_when_history_is_already_materialized()
    {
        await using var context = CreateContext();
        await SeedTransactionsAsync(context,
            (new DateTimeOffset(2026, 3, 2, 10, 0, 0, TimeSpan.Zero), -2, StockTransactionType.Usage));
        context.DailyItemConsumptions.Add(new DailyItemConsumption { Date = new DateOnly(2026, 3, 2), Quantity = 2 });
        await context.SaveChangesAsync(CancellationToken.None);

        (await HandleAsync(context)).ShouldBeNull();
    }

    [Test]
    public async Task Returns_the_earliest_date_when_materialized_rows_start_later()
    {
        await using var context = CreateContext();
        await SeedTransactionsAsync(context,
            (new DateTimeOffset(2026, 3, 2, 10, 0, 0, TimeSpan.Zero), -2, StockTransactionType.Usage));
        context.DailyItemConsumptions.Add(new DailyItemConsumption { Date = new DateOnly(2026, 8, 1), Quantity = 1 });
        await context.SaveChangesAsync(CancellationToken.None);

        (await HandleAsync(context)).ShouldBe(new DateOnly(2026, 3, 2));
    }
}

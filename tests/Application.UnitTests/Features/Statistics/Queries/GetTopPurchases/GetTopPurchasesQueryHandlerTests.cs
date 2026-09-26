using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;
using skestock.Application.Features.Statistics.Queries.GetItemsPurchaseHistory;
using skestock.Application.Features.Statistics.Queries.GetTopPurchases;
using skestock.Application.UnitTests.Features.GoodsReceipts.Commands.CreateGoodsReceipt;
using skestock.Domain.Entities;
using skestock.Domain.Enums;

namespace skestock.Application.UnitTests.Features.Statistics.Queries.GetTopPurchases;

[TestFixture]
public sealed class GetTopPurchasesQueryHandlerTests
{
    private static GoodsReceiptTestDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<GoodsReceiptTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private sealed record Seed(SchoolClass Class, Category Paper, Category Food, Item Pens, Item Sheets, Item Apples);

    private static async Task<Seed> SeedAsync(GoodsReceiptTestDbContext context)
    {
        var schoolClass = new SchoolClass
        {
            Name = "Class", StartDate = new DateOnly(2026, 1, 1), EndDate = new DateOnly(2026, 12, 31)
        };
        var paper = new Category { Name = "Papetărie" };
        var food = new Category { Name = "Alimente" };
        var pens = new Item { Name = "Pixuri", Unit = "buc", Category = paper };
        var sheets = new Item { Name = "Coli", Unit = "top", Category = paper };
        var apples = new Item { Name = "Mere", Unit = "kg", Category = food };
        context.AddRange(schoolClass, pens, sheets, apples);

        // Pens: most units, Apples: most value, Sheets: most purchases.
        context.AddRange(
            Statistic(PurchaseStatisticsScope.Last365Days, null, pens, 100, 50m, 2),
            Statistic(PurchaseStatisticsScope.Last365Days, null, sheets, 20, 80m, 6),
            Statistic(PurchaseStatisticsScope.Last365Days, null, apples, 40, 200m, 3),
            Statistic(PurchaseStatisticsScope.Last90Days, null, pens, 5, 1m, 1),
            Statistic(PurchaseStatisticsScope.Class, schoolClass, apples, 7, 9m, 1));
        await context.SaveChangesAsync(CancellationToken.None);
        return new Seed(schoolClass, paper, food, pens, sheets, apples);
    }

    private static ItemPurchaseStatistic Statistic(
        PurchaseStatisticsScope scope, SchoolClass? schoolClass, Item item, int quantity, decimal value, int count) =>
        new()
        {
            Scope = scope,
            Class = schoolClass,
            Item = item,
            TotalQuantity = quantity,
            TotalValue = value,
            PurchaseCount = count,
            AverageQuantity = (decimal)quantity / count,
            LastPurchasedAt = new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero),
            ComputedAt = new DateTimeOffset(2026, 9, 26, 18, 0, 0, TimeSpan.Zero)
        };

    [Test]
    public async Task Ranks_items_by_quantity_value_and_frequency()
    {
        await using var context = CreateContext();
        var seed = await SeedAsync(context);

        var result = await new GetTopPurchasesQueryHandler(context).Handle(
            new GetTopPurchasesQuery { Scope = PurchaseStatisticsScope.Last365Days },
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ByQuantity.Select(s => s.ItemId).ShouldBe([seed.Pens.Id, seed.Apples.Id, seed.Sheets.Id]);
        result.Value.ByValue[0].ItemId.ShouldBe(seed.Apples.Id);
        result.Value.ByFrequency[0].ItemId.ShouldBe(seed.Sheets.Id);
        result.Value.ByQuantity[0].Unit.ShouldBe("buc");
        result.Value.ByQuantity[0].CategoryName.ShouldBe("Papetărie");
        result.Value.ComputedAt.ShouldNotBeNull();
    }

    [Test]
    public async Task Filters_by_category_and_limits_to_top()
    {
        await using var context = CreateContext();
        var seed = await SeedAsync(context);

        var result = await new GetTopPurchasesQueryHandler(context).Handle(
            new GetTopPurchasesQuery
            {
                Scope = PurchaseStatisticsScope.Last365Days, CategoryId = seed.Paper.Id, Top = 1
            },
            CancellationToken.None);

        result.Value.ByQuantity.Single().ItemId.ShouldBe(seed.Pens.Id);
        result.Value.ByValue.Single().ItemId.ShouldBe(seed.Sheets.Id);
    }

    [Test]
    public async Task Class_scope_returns_only_that_class()
    {
        await using var context = CreateContext();
        var seed = await SeedAsync(context);

        var result = await new GetTopPurchasesQueryHandler(context).Handle(
            new GetTopPurchasesQuery { Scope = PurchaseStatisticsScope.Class, ClassId = seed.Class.Id },
            CancellationToken.None);

        result.Value.ByQuantity.Single().ItemId.ShouldBe(seed.Apples.Id);
    }

    [Test]
    public async Task Empty_scope_returns_empty_lists()
    {
        await using var context = CreateContext();

        var result = await new GetTopPurchasesQueryHandler(context).Handle(
            new GetTopPurchasesQuery(), CancellationToken.None);

        result.Value.ByQuantity.ShouldBeEmpty();
        result.Value.ComputedAt.ShouldBeNull();
    }

    [Test]
    public async Task Items_history_returns_last_365_days_rows_for_requested_items()
    {
        await using var context = CreateContext();
        var seed = await SeedAsync(context);

        var result = await new GetItemsPurchaseHistoryQueryHandler(context).Handle(
            new GetItemsPurchaseHistoryQuery { ItemIds = [seed.Pens.Id, seed.Pens.Id, Guid.NewGuid()] },
            CancellationToken.None);

        var history = result.Value.ShouldHaveSingleItem();
        history.ItemId.ShouldBe(seed.Pens.Id);
        history.PurchaseCount.ShouldBe(2);
        history.TotalQuantity.ShouldBe(100);
    }
}

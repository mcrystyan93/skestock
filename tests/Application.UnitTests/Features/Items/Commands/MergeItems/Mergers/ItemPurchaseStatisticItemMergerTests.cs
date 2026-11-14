using NUnit.Framework;
using Shouldly;
using skestock.Application.Features.Items.Commands.MergeItems.Mergers;
using skestock.Domain.Entities.Statistics;
using skestock.Domain.Enums;

namespace skestock.Application.UnitTests.Features.Items.Commands.MergeItems.Mergers;

public class ItemPurchaseStatisticItemMergerTests
{
    private static readonly Guid Primary = Guid.NewGuid();
    private static readonly Guid SecondaryA = Guid.NewGuid();
    private static readonly Guid SecondaryB = Guid.NewGuid();
    private static readonly Guid Class1 = Guid.NewGuid();
    private static readonly DateTimeOffset Early = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Late = Early.AddMonths(3);

    private static ItemPurchaseStatistic Stat(
        Guid item, int qty, decimal value, int count, DateTimeOffset last, decimal? interval = null,
        Guid? classId = null, PurchaseStatisticsScope scope = PurchaseStatisticsScope.Last365Days, int? lastQuantity = null) =>
        new()
        {
            ItemId = item, Scope = scope, ClassId = classId, TotalQuantity = qty, TotalValue = value,
            PurchaseCount = count, LastPurchasedAt = last, LastQuantity = lastQuantity, AverageIntervalDays = interval, ComputedAt = last
        };

    [Test]
    public async Task PrimaryCollision_SumsAndRecomputesAverages()
    {
        await using var db = MergeTestData.NewContext();
        db.ItemPurchaseStatistics.AddRange(
            Stat(Primary, 10, 20m, 2, Early, interval: 12.5m, lastQuantity: 4),
            Stat(SecondaryA, 5, 10m, 1, Late, interval: 99m, lastQuantity: 5));
        await db.SaveChangesAsync();
        var merger = new ItemPurchaseStatisticItemMerger(db);

        (await merger.PreviewAsync(Primary, [SecondaryA], default))
            .ShouldBe(new ItemMergeImpact(ItemMergeKind.ItemPurchaseStatistics, 0, 1));
        await merger.MergeAsync(Primary, [SecondaryA], default);
        await db.SaveChangesAsync();

        var row = db.ItemPurchaseStatistics.Single();
        row.ItemId.ShouldBe(Primary);
        row.TotalQuantity.ShouldBe(15);
        row.TotalValue.ShouldBe(30m);
        row.PurchaseCount.ShouldBe(3);
        row.AverageQuantity.ShouldBe(5m);
        row.AverageUnitPrice.ShouldBe(2m);
        row.LastPurchasedAt.ShouldBe(Late);
        row.AverageIntervalDays.ShouldBe(12.5m);
        row.LastQuantity.ShouldBe(5);
    }

    [Test]
    public async Task Averages_AreRoundedToTwoDecimals()
    {
        await using var db = MergeTestData.NewContext();
        db.ItemPurchaseStatistics.AddRange(Stat(Primary, 3, 10m, 1, Early), Stat(SecondaryA, 4, 10m, 1, Early));
        await db.SaveChangesAsync();

        await new ItemPurchaseStatisticItemMerger(db).MergeAsync(Primary, [SecondaryA], default);
        await db.SaveChangesAsync();

        var row = db.ItemPurchaseStatistics.Single();
        row.AverageQuantity.ShouldBe(3.5m);
        row.AverageUnitPrice.ShouldBe(2.86m); // 20 / 7
    }

    [Test]
    public async Task ZeroQuantityAndZeroCount_DoNotDivideByZero()
    {
        await using var db = MergeTestData.NewContext();
        db.ItemPurchaseStatistics.AddRange(Stat(Primary, 0, 0m, 0, Early), Stat(SecondaryA, 0, 0m, 0, Early));
        await db.SaveChangesAsync();

        await new ItemPurchaseStatisticItemMerger(db).MergeAsync(Primary, [SecondaryA], default);
        await db.SaveChangesAsync();

        var row = db.ItemPurchaseStatistics.Single();
        (row.AverageQuantity, row.AverageUnitPrice).ShouldBe((0m, 0m));
    }

    [Test]
    public async Task NullClassId_CollidesOnlyWithinSameScope()
    {
        await using var db = MergeTestData.NewContext();
        db.ItemPurchaseStatistics.AddRange(
            Stat(Primary, 1, 1m, 1, Early),                                              // global window
            Stat(SecondaryA, 1, 1m, 1, Early),                                           // collides (same scope, null class)
            Stat(SecondaryA, 1, 1m, 1, Early, classId: Class1, scope: PurchaseStatisticsScope.Class)); // no collision
        await db.SaveChangesAsync();

        await new ItemPurchaseStatisticItemMerger(db).MergeAsync(Primary, [SecondaryA], default);
        await db.SaveChangesAsync();

        db.ItemPurchaseStatistics.Count().ShouldBe(2);
        db.ItemPurchaseStatistics.ShouldAllBe(r => r.ItemId == Primary);
        db.ItemPurchaseStatistics.Single(r => r.ClassId == null).TotalQuantity.ShouldBe(2);
    }

    [Test]
    public async Task SecondaryVsSecondaryCollision_KeepsOneRepointedRow()
    {
        await using var db = MergeTestData.NewContext();
        db.ItemPurchaseStatistics.AddRange(Stat(SecondaryA, 2, 4m, 1, Early), Stat(SecondaryB, 3, 6m, 1, Late));
        await db.SaveChangesAsync();

        await new ItemPurchaseStatisticItemMerger(db).MergeAsync(Primary, [SecondaryA, SecondaryB], default);
        await db.SaveChangesAsync();

        var row = db.ItemPurchaseStatistics.Single();
        (row.ItemId, row.TotalQuantity, row.PurchaseCount, row.LastPurchasedAt).ShouldBe((Primary, 5, 2, Late));
    }
}

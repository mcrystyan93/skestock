using NUnit.Framework;
using Shouldly;
using skestock.Application.Features.Items.Commands.MergeItems.Mergers;
using skestock.Domain.Entities.Statistics;
using skestock.Domain.Entities.Stock;
using skestock.Domain.Entities.SupplyLists;

namespace skestock.Application.UnitTests.Features.Items.Commands.MergeItems.Mergers;

public class CombiningItemMergerTests
{
    private static readonly Guid Primary = Guid.NewGuid();
    private static readonly Guid SecondaryA = Guid.NewGuid();
    private static readonly Guid SecondaryB = Guid.NewGuid();
    private static readonly Guid List1 = Guid.NewGuid();
    private static readonly Guid Class1 = Guid.NewGuid();
    private static readonly Guid Loc1 = Guid.NewGuid();
    private static readonly Guid Loc2 = Guid.NewGuid();
    private static readonly DateOnly Day = new(2026, 1, 5);

    private static SupplyListLine Line(Guid item, decimal qty, string? notes = null) =>
        new() { SupplyListId = List1, ItemId = item, Quantity = qty, Unit = "buc", Notes = notes };

    private static DailyItemConsumption Daily(Guid item, int qty, decimal value, DateTimeOffset at) =>
        new() { Date = Day, ItemId = item, ClassId = Class1, LocationId = Loc1, Quantity = qty, TotalValue = value, ComputedAt = at };

    private static ClassBalance Balance(Guid item, Guid loc, int opening, int? closing) =>
        new() { ClassId = Class1, ItemId = item, LocationId = loc, OpeningQty = opening, ClosingQty = closing };

    // SupplyListLine

    [Test]
    public async Task SupplyLine_NoCollision_Repoints()
    {
        await using var db = MergeTestData.NewContext();
        db.SupplyListLines.Add(Line(SecondaryA, 3));
        await db.SaveChangesAsync();

        await new SupplyListLineItemMerger(db).MergeAsync(Primary, [SecondaryA], default);
        await db.SaveChangesAsync();

        db.SupplyListLines.Single().ItemId.ShouldBe(Primary);
    }

    [Test]
    public async Task SupplyLine_PrimaryCollision_SumsQuantityIntoPrimaryLine()
    {
        await using var db = MergeTestData.NewContext();
        db.SupplyListLines.AddRange(Line(Primary, 2, "keep"), Line(SecondaryA, 3, "drop"));
        await db.SaveChangesAsync();
        var merger = new SupplyListLineItemMerger(db);

        (await merger.PreviewAsync(Primary, [SecondaryA], default)).ShouldBe(new ItemMergeImpact(ItemMergeKind.SupplyListLines, 0, 1));
        await merger.MergeAsync(Primary, [SecondaryA], default);
        await db.SaveChangesAsync();

        var line = db.SupplyListLines.Single();
        line.ItemId.ShouldBe(Primary);
        line.Quantity.ShouldBe(5);
        line.Notes.ShouldBe("keep");
    }

    [Test]
    public async Task SupplyLine_SecondaryVsSecondaryCollision_SumsIntoOneRepointedLine()
    {
        await using var db = MergeTestData.NewContext();
        db.SupplyListLines.AddRange(Line(SecondaryA, 2), Line(SecondaryB, 4));
        await db.SaveChangesAsync();

        await new SupplyListLineItemMerger(db).MergeAsync(Primary, [SecondaryA, SecondaryB], default);
        await db.SaveChangesAsync();

        var line = db.SupplyListLines.Single();
        line.ItemId.ShouldBe(Primary);
        line.Quantity.ShouldBe(6);
    }

    // DailyItemConsumption

    [Test]
    public async Task Daily_PrimaryCollision_SumsQuantityAndValueAndTakesLatestComputedAt()
    {
        var early = new DateTimeOffset(2026, 1, 6, 0, 0, 0, TimeSpan.Zero);
        var late = early.AddDays(1);
        await using var db = MergeTestData.NewContext();
        db.DailyItemConsumptions.AddRange(Daily(Primary, 2, 4.5m, early), Daily(SecondaryA, 3, 1.25m, late));
        await db.SaveChangesAsync();
        var merger = new DailyItemConsumptionItemMerger(db);

        (await merger.PreviewAsync(Primary, [SecondaryA], default)).ShouldBe(new ItemMergeImpact(ItemMergeKind.DailyItemConsumptions, 0, 1));
        await merger.MergeAsync(Primary, [SecondaryA], default);
        await db.SaveChangesAsync();

        var row = db.DailyItemConsumptions.Single();
        row.ItemId.ShouldBe(Primary);
        row.Quantity.ShouldBe(5);
        row.TotalValue.ShouldBe(5.75m);
        row.ComputedAt.ShouldBe(late);
    }

    [Test]
    public async Task Daily_SecondaryVsSecondaryCollision_SumsIntoOneRepointedRow()
    {
        var at = new DateTimeOffset(2026, 1, 6, 0, 0, 0, TimeSpan.Zero);
        await using var db = MergeTestData.NewContext();
        db.DailyItemConsumptions.AddRange(Daily(SecondaryA, 1, 1m, at), Daily(SecondaryB, 2, 2m, at));
        await db.SaveChangesAsync();

        await new DailyItemConsumptionItemMerger(db).MergeAsync(Primary, [SecondaryA, SecondaryB], default);
        await db.SaveChangesAsync();

        var row = db.DailyItemConsumptions.Single();
        (row.ItemId, row.Quantity, row.TotalValue).ShouldBe((Primary, 3, 3m));
    }

    [Test]
    public async Task Daily_DifferentLocation_DoesNotCollide()
    {
        var at = DateTimeOffset.UtcNow;
        await using var db = MergeTestData.NewContext();
        var other = Daily(SecondaryA, 1, 1m, at);
        other.LocationId = Loc2;
        db.DailyItemConsumptions.AddRange(Daily(Primary, 2, 2m, at), other);
        await db.SaveChangesAsync();

        await new DailyItemConsumptionItemMerger(db).MergeAsync(Primary, [SecondaryA], default);
        await db.SaveChangesAsync();

        db.DailyItemConsumptions.Count().ShouldBe(2);
        db.DailyItemConsumptions.ShouldAllBe(r => r.ItemId == Primary);
    }

    // ClassBalance

    [Test]
    public async Task Balance_PrimaryCollision_SumsOpeningAndClosing()
    {
        await using var db = MergeTestData.NewContext();
        db.ClassBalances.AddRange(Balance(Primary, Loc1, 4, 3), Balance(SecondaryA, Loc1, 6, 2), Balance(SecondaryA, Loc2, 1, null));
        await db.SaveChangesAsync();
        var merger = new ClassBalanceItemMerger(db);

        (await merger.PreviewAsync(Primary, [SecondaryA], default)).ShouldBe(new ItemMergeImpact(ItemMergeKind.ClassBalances, 1, 1));
        await merger.MergeAsync(Primary, [SecondaryA], default);
        await db.SaveChangesAsync();

        db.ClassBalances.Count().ShouldBe(2);
        var merged = db.ClassBalances.Single(b => b.LocationId == Loc1);
        (merged.ItemId, merged.OpeningQty, merged.ClosingQty).ShouldBe((Primary, 10, 5));
        var moved = db.ClassBalances.Single(b => b.LocationId == Loc2);
        (moved.ItemId, moved.OpeningQty, moved.ClosingQty).ShouldBe((Primary, 1, null));
    }

    [TestCase(null, null, null)]
    [TestCase(3, null, 3)]
    [TestCase(null, 2, 2)]
    [TestCase(3, 2, 5)]
    public async Task Balance_ClosingQty_IsNullOnlyWhenBothNull(int? primaryClosing, int? secondaryClosing, int? expected)
    {
        await using var db = MergeTestData.NewContext();
        db.ClassBalances.AddRange(Balance(Primary, Loc1, 1, primaryClosing), Balance(SecondaryA, Loc1, 1, secondaryClosing));
        await db.SaveChangesAsync();

        await new ClassBalanceItemMerger(db).MergeAsync(Primary, [SecondaryA], default);
        await db.SaveChangesAsync();

        db.ClassBalances.Single().ClosingQty.ShouldBe(expected);
    }

    [Test]
    public async Task Balance_SecondaryVsSecondaryCollision_KeepsPerLocationTotals()
    {
        await using var db = MergeTestData.NewContext();
        db.ClassBalances.AddRange(Balance(SecondaryA, Loc1, 2, 1), Balance(SecondaryB, Loc1, 5, 4));
        await db.SaveChangesAsync();

        await new ClassBalanceItemMerger(db).MergeAsync(Primary, [SecondaryA, SecondaryB], default);
        await db.SaveChangesAsync();

        var row = db.ClassBalances.Single();
        (row.ItemId, row.OpeningQty, row.ClosingQty).ShouldBe((Primary, 7, 5));
    }
}

using NUnit.Framework;
using Shouldly;
using skestock.Application.Features.Items.Commands.MergeItems.Mergers;
using skestock.Domain.Entities.Items;
using skestock.Domain.Entities.Stock;

namespace skestock.Application.UnitTests.Features.Items.Commands.MergeItems.Mergers;

public class KeepSurvivorItemMergerTests
{
    private static readonly Guid Primary = Guid.NewGuid();
    private static readonly Guid SecondaryA = Guid.NewGuid();
    private static readonly Guid SecondaryB = Guid.NewGuid();
    private static readonly Guid Class1 = Guid.NewGuid();
    private static readonly Guid Loc1 = Guid.NewGuid();
    private static readonly Guid Loc2 = Guid.NewGuid();

    private static ClassItemStockVisibility Vis(Guid item, Guid loc, bool hide) =>
        new() { ItemId = item, ClassId = Class1, LocationId = loc, HideWhenZeroStock = hide };

    [Test]
    public async Task Visibility_NoCollision_Repoints()
    {
        await using var db = MergeTestData.NewContext();
        db.ClassItemStockVisibilities.Add(Vis(SecondaryA, Loc1, true));
        await db.SaveChangesAsync();
        var merger = new ClassItemStockVisibilityItemMerger(db);

        (await merger.PreviewAsync(Primary, [SecondaryA], default))
            .ShouldBe(new ItemMergeImpact(ItemMergeKind.ClassItemStockVisibilities, 1, 0));
        await merger.MergeAsync(Primary, [SecondaryA], default);
        await db.SaveChangesAsync();

        db.ClassItemStockVisibilities.Single().ItemId.ShouldBe(Primary);
    }

    [Test]
    public async Task Visibility_PrimaryCollision_KeepsPrimaryRowAndDeletesSecondary()
    {
        await using var db = MergeTestData.NewContext();
        db.ClassItemStockVisibilities.AddRange(Vis(Primary, Loc1, false), Vis(SecondaryA, Loc1, true), Vis(SecondaryA, Loc2, true));
        await db.SaveChangesAsync();
        var merger = new ClassItemStockVisibilityItemMerger(db);

        (await merger.PreviewAsync(Primary, [SecondaryA], default))
            .ShouldBe(new ItemMergeImpact(ItemMergeKind.ClassItemStockVisibilities, 1, 1));
        await merger.MergeAsync(Primary, [SecondaryA], default);
        await db.SaveChangesAsync();

        db.ClassItemStockVisibilities.Count().ShouldBe(2);
        db.ClassItemStockVisibilities.ShouldAllBe(v => v.ItemId == Primary);
        db.ClassItemStockVisibilities.Single(v => v.LocationId == Loc1).HideWhenZeroStock.ShouldBeFalse();
    }

    [Test]
    public async Task Visibility_SecondaryVsSecondaryCollision_KeepsOneRepointedRow()
    {
        await using var db = MergeTestData.NewContext();
        db.ClassItemStockVisibilities.AddRange(Vis(SecondaryA, Loc1, true), Vis(SecondaryB, Loc1, true));
        await db.SaveChangesAsync();
        var merger = new ClassItemStockVisibilityItemMerger(db);

        (await merger.PreviewAsync(Primary, [SecondaryA, SecondaryB], default))
            .ShouldBe(new ItemMergeImpact(ItemMergeKind.ClassItemStockVisibilities, 1, 1));
        await merger.MergeAsync(Primary, [SecondaryA, SecondaryB], default);
        await db.SaveChangesAsync();

        db.ClassItemStockVisibilities.Single().ItemId.ShouldBe(Primary);
    }

    [Test]
    public async Task Threshold_PrimaryCollision_KeepsPrimaryRow()
    {
        await using var db = MergeTestData.NewContext();
        db.ItemLocationThresholds.AddRange(
            new ItemLocationThreshold { ItemId = Primary, LocationId = Loc1, MinThreshold = 5 },
            new ItemLocationThreshold { ItemId = SecondaryA, LocationId = Loc1, MinThreshold = 99 },
            new ItemLocationThreshold { ItemId = SecondaryA, LocationId = Loc2, MinThreshold = 7 });
        await db.SaveChangesAsync();
        var merger = new ItemLocationThresholdItemMerger(db);

        (await merger.PreviewAsync(Primary, [SecondaryA], default))
            .ShouldBe(new ItemMergeImpact(ItemMergeKind.ItemLocationThresholds, 1, 1));
        await merger.MergeAsync(Primary, [SecondaryA], default);
        await db.SaveChangesAsync();

        db.ItemLocationThresholds.Count().ShouldBe(2);
        db.ItemLocationThresholds.Single(t => t.LocationId == Loc1).MinThreshold.ShouldBe(5);
        db.ItemLocationThresholds.Single(t => t.LocationId == Loc2).ItemId.ShouldBe(Primary);
    }

    [Test]
    public async Task Threshold_SecondaryVsSecondaryCollision_KeepsOneRepointedRow()
    {
        await using var db = MergeTestData.NewContext();
        db.ItemLocationThresholds.AddRange(
            new ItemLocationThreshold { ItemId = SecondaryA, LocationId = Loc1, MinThreshold = 1 },
            new ItemLocationThreshold { ItemId = SecondaryB, LocationId = Loc1, MinThreshold = 2 });
        await db.SaveChangesAsync();
        var merger = new ItemLocationThresholdItemMerger(db);

        await merger.MergeAsync(Primary, [SecondaryA, SecondaryB], default);
        await db.SaveChangesAsync();

        db.ItemLocationThresholds.Single().ItemId.ShouldBe(Primary);
    }
}

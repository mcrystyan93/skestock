using NUnit.Framework;
using Shouldly;
using skestock.Application.Features.Items.Commands.MergeItems.Mergers;
using skestock.Domain.Entities.GoodsReceipts;
using skestock.Domain.Entities.OrderLists;
using skestock.Domain.Entities.Stock;

namespace skestock.Application.UnitTests.Features.Items.Commands.MergeItems.Mergers;

public class PlainItemMergerTests
{
    private static readonly Guid Primary = Guid.NewGuid();
    private static readonly Guid Secondary = Guid.NewGuid();
    private static readonly Guid Other = Guid.NewGuid();

    [Test]
    public async Task StockBatch_RepointsOnlySecondaryRows()
    {
        await using var db = MergeTestData.NewContext();
        db.StockBatches.AddRange(
            new StockBatch { ItemId = Secondary }, new StockBatch { ItemId = Secondary },
            new StockBatch { ItemId = Primary }, new StockBatch { ItemId = Other });
        await db.SaveChangesAsync();
        var merger = new StockBatchItemMerger(db);

        (await merger.PreviewAsync(Primary, [Secondary], default)).ShouldBe(new ItemMergeImpact(ItemMergeKind.StockBatches, 2, 0));
        await merger.MergeAsync(Primary, [Secondary], default);
        await db.SaveChangesAsync();

        db.StockBatches.Count(b => b.ItemId == Primary).ShouldBe(3);
        db.StockBatches.Count(b => b.ItemId == Secondary).ShouldBe(0);
        db.StockBatches.Count(b => b.ItemId == Other).ShouldBe(1);
    }

    [Test]
    public async Task StockTransaction_RepointsOnlySecondaryRows()
    {
        await using var db = MergeTestData.NewContext();
        db.StockTransactions.AddRange(new StockTransaction { ItemId = Secondary }, new StockTransaction { ItemId = Other });
        await db.SaveChangesAsync();
        var merger = new StockTransactionItemMerger(db);

        (await merger.PreviewAsync(Primary, [Secondary], default)).ShouldBe(new ItemMergeImpact(ItemMergeKind.StockTransactions, 1, 0));
        await merger.MergeAsync(Primary, [Secondary], default);
        await db.SaveChangesAsync();

        db.StockTransactions.Single(t => t.ItemId == Primary).ShouldNotBeNull();
        db.StockTransactions.Count(t => t.ItemId == Other).ShouldBe(1);
    }

    [Test]
    public async Task GoodsReceiptImportLine_RepointsMatchedItemAndIgnoresUnmatched()
    {
        await using var db = MergeTestData.NewContext();
        db.GoodsReceiptImportLines.AddRange(
            new GoodsReceiptImportLine { RawItemText = "a", MatchedItemId = Secondary },
            new GoodsReceiptImportLine { RawItemText = "b", MatchedItemId = null },
            new GoodsReceiptImportLine { RawItemText = "c", MatchedItemId = Other });
        await db.SaveChangesAsync();
        var merger = new GoodsReceiptImportLineItemMerger(db);

        (await merger.PreviewAsync(Primary, [Secondary], default)).ShouldBe(new ItemMergeImpact(ItemMergeKind.GoodsReceiptImportLines, 1, 0));
        await merger.MergeAsync(Primary, [Secondary], default);
        await db.SaveChangesAsync();

        db.GoodsReceiptImportLines.Single(l => l.RawItemText == "a").MatchedItemId.ShouldBe(Primary);
        db.GoodsReceiptImportLines.Single(l => l.RawItemText == "b").MatchedItemId.ShouldBeNull();
        db.GoodsReceiptImportLines.Single(l => l.RawItemText == "c").MatchedItemId.ShouldBe(Other);
    }

    [Test]
    public async Task OrderListLine_RepointsItemButKeepsNameSnapshotAndNullItem()
    {
        await using var db = MergeTestData.NewContext();
        db.OrderListLines.AddRange(
            new OrderListLine { ItemId = Secondary, ProductName = "Old name" },
            new OrderListLine { ItemId = null, ProductName = "Free text" });
        await db.SaveChangesAsync();
        var merger = new OrderListLineItemMerger(db);

        (await merger.PreviewAsync(Primary, [Secondary], default)).ShouldBe(new ItemMergeImpact(ItemMergeKind.OrderListLines, 1, 0));
        await merger.MergeAsync(Primary, [Secondary], default);
        await db.SaveChangesAsync();

        var moved = db.OrderListLines.Single(l => l.ProductName == "Old name");
        moved.ItemId.ShouldBe(Primary);
        db.OrderListLines.Single(l => l.ProductName == "Free text").ItemId.ShouldBeNull();
    }

    [Test]
    public async Task PreviewDoesNotChangeAnything()
    {
        await using var db = MergeTestData.NewContext();
        db.StockBatches.Add(new StockBatch { ItemId = Secondary });
        await db.SaveChangesAsync();

        await new StockBatchItemMerger(db).PreviewAsync(Primary, [Secondary], default);

        db.ChangeTracker.HasChanges().ShouldBeFalse();
        db.StockBatches.Single().ItemId.ShouldBe(Secondary);
    }
}

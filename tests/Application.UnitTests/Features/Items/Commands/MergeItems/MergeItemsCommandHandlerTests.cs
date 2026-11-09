using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;
using skestock.Application.Common.Errors;
using skestock.Application.Common.Behaviours;
using skestock.Application.Common.Security;
using skestock.Application.Features.Items.Commands.MergeItems;
using skestock.Application.Features.Items.Commands.MergeItems.Mergers;
using skestock.Domain.Constants;
using skestock.Domain.Entities.GoodsReceipts;
using skestock.Domain.Entities.Items;
using skestock.Domain.Entities.OrderLists;
using skestock.Domain.Entities.SchoolClasses;
using skestock.Domain.Entities.Statistics;
using skestock.Domain.Entities.Stock;
using skestock.Domain.Entities.SupplyLists;
using skestock.Domain.Enums;
using skestock.Domain.Events.Items;

namespace skestock.Application.UnitTests.Features.Items.Commands.MergeItems;

public class MergeItemsCommandHandlerTests
{
    private static MergeItemsCommandHandler HandlerFor(MergeTestDbContext db) =>
        new(db, [
            new StockBatchItemMerger(db), new StockTransactionItemMerger(db), new GoodsReceiptImportLineItemMerger(db),
            new OrderListLineItemMerger(db), new ClassItemStockVisibilityItemMerger(db),
            new ItemLocationThresholdItemMerger(db), new SupplyListLineItemMerger(db),
            new DailyItemConsumptionItemMerger(db), new ClassBalanceItemMerger(db),
            new ItemPurchaseStatisticItemMerger(db)
        ]);

    private static async Task<(MergeTestDbContext Db, Item Primary, Item Secondary)> SeedAsync(bool primaryActive = true)
    {
        var db = MergeTestData.NewContext();
        var primary = MergeTestData.NewItem("Pencil", primaryActive);
        var secondary = MergeTestData.NewItem("Pencil (dup)");
        db.Items.AddRange(primary, secondary);
        db.SchoolClasses.Add(new SchoolClass { Name = "1A" });
        var (classId, locationId, listId) = (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        db.StockBatches.Add(new StockBatch { ItemId = secondary.Id });
        db.StockTransactions.Add(new StockTransaction { ItemId = secondary.Id });
        db.GoodsReceiptImportLines.Add(new GoodsReceiptImportLine { RawItemText = "x", MatchedItemId = secondary.Id });
        db.OrderListLines.Add(new OrderListLine { ItemId = secondary.Id, ProductName = "x" });
        db.ClassItemStockVisibilities.Add(new ClassItemStockVisibility { ItemId = secondary.Id, ClassId = classId, LocationId = locationId });
        db.ItemLocationThresholds.Add(new ItemLocationThreshold { ItemId = secondary.Id, LocationId = locationId });
        db.SupplyListLines.Add(new SupplyListLine { ItemId = secondary.Id, SupplyListId = listId, Unit = "u" });
        db.DailyItemConsumptions.Add(new DailyItemConsumption { ItemId = secondary.Id, ClassId = classId, LocationId = locationId });
        db.ClassBalances.Add(new ClassBalance { ItemId = secondary.Id, ClassId = classId, LocationId = locationId });
        db.ItemPurchaseStatistics.Add(new ItemPurchaseStatistic { ItemId = secondary.Id, Scope = PurchaseStatisticsScope.Last365Days });
        await db.SaveChangesAsync();
        return (db, primary, secondary);
    }

    private static MergeItemsCommand Cmd(Item primary, params Item[] secondaries) =>
        new(primary.Id, [primary.Id, .. secondaries.Select(s => s.Id)]);

    // A transient SaveChanges failure makes the execution strategy rerun the handler on the same context, with the
    // failed attempt's combined survivors and deleted rows still tracked.
    [Test]
    public async Task Handle_RerunAfterFailedAttempt_DoesNotCombineTwice()
    {
        var (db, primary, secondary) = await SeedAsync();
        await using var _ = db;
        var (classId, locationId) = (Guid.NewGuid(), Guid.NewGuid());
        db.ClassBalances.RemoveRange(db.ClassBalances);
        db.ClassBalances.AddRange(
            new ClassBalance { ItemId = primary.Id, ClassId = classId, LocationId = locationId, OpeningQty = 5 },
            new ClassBalance { ItemId = secondary.Id, ClassId = classId, LocationId = locationId, OpeningQty = 3 });
        await db.SaveChangesAsync();
        await new ClassBalanceItemMerger(db).MergeAsync(primary.Id, [secondary.Id], default);

        var result = await HandlerFor(db).Handle(Cmd(primary, secondary), default);

        result.IsSuccess.ShouldBeTrue();
        db.ClassBalances.AsNoTracking().Single().OpeningQty.ShouldBe(8);
    }

    [Test]
    public async Task Handle_RepointsEveryTableDisablesSecondaryAndRaisesEvent()
    {
        var (db, primary, secondary) = await SeedAsync();
        await using var _ = db;

        var result = await HandlerFor(db).Handle(Cmd(primary, secondary), default);

        result.IsSuccess.ShouldBeTrue();
        db.StockBatches.Single().ItemId.ShouldBe(primary.Id);
        db.StockTransactions.Single().ItemId.ShouldBe(primary.Id);
        db.GoodsReceiptImportLines.Single().MatchedItemId.ShouldBe(primary.Id);
        db.OrderListLines.Single().ItemId.ShouldBe(primary.Id);
        db.ClassItemStockVisibilities.Single().ItemId.ShouldBe(primary.Id);
        db.ItemLocationThresholds.Single().ItemId.ShouldBe(primary.Id);
        db.SupplyListLines.Single().ItemId.ShouldBe(primary.Id);
        db.DailyItemConsumptions.Single().ItemId.ShouldBe(primary.Id);
        db.ClassBalances.Single().ItemId.ShouldBe(primary.Id);
        db.ItemPurchaseStatistics.Single().ItemId.ShouldBe(primary.Id);
        // The handler clears the change tracker, so it works on freshly loaded instances.
        var reloadedSecondary = (await db.Items.FindAsync(secondary.Id))!;
        var reloadedPrimary = (await db.Items.FindAsync(primary.Id))!;
        reloadedSecondary.IsActive.ShouldBeFalse();
        reloadedPrimary.IsActive.ShouldBeTrue();
        reloadedSecondary.DomainEvents.OfType<ItemDisabledEvent>().ShouldHaveSingleItem().Item.ShouldBe(reloadedSecondary);
        reloadedPrimary.DomainEvents.OfType<ItemDisabledEvent>().ShouldBeEmpty();
    }

    [Test]
    public async Task Handle_WithUnknownItem_ReturnsItemNotFound()
    {
        var (db, primary, _) = await SeedAsync();
        await using var _db = db;
        var missing = Guid.NewGuid();

        var result = await HandlerFor(db).Handle(new MergeItemsCommand(primary.Id, [primary.Id, missing]), default);

        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<ItemErrors.ItemNotFound>();
    }

    [Test]
    public async Task Handle_WithInactivePrimary_FailsAndChangesNothing()
    {
        var (db, primary, secondary) = await SeedAsync(primaryActive: false);
        await using var _ = db;

        var result = await HandlerFor(db).Handle(Cmd(primary, secondary), default);

        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<ItemErrors.MergePrimaryInactive>();
        db.StockBatches.Single().ItemId.ShouldBe(secondary.Id);
        (await db.Items.FindAsync(secondary.Id))!.IsActive.ShouldBeTrue();
        db.ChangeTracker.HasChanges().ShouldBeFalse();
    }

    [Test]
    public async Task Handle_WithAlreadyInactiveSecondary_Succeeds()
    {
        var (db, primary, secondary) = await SeedAsync();
        await using var _ = db;
        secondary.IsActive = false;
        await db.SaveChangesAsync();

        var result = await HandlerFor(db).Handle(Cmd(primary, secondary), default);

        result.IsSuccess.ShouldBeTrue();
        db.StockBatches.Single().ItemId.ShouldBe(primary.Id);
    }

    [Test]
    public async Task Handle_RunTwice_SecondRunSucceedsWithNoReferenceChanges()
    {
        var (db, primary, secondary) = await SeedAsync();
        await using var _ = db;
        var handler = HandlerFor(db);
        await handler.Handle(Cmd(primary, secondary), default);

        var second = await handler.Handle(Cmd(primary, secondary), default);

        second.IsSuccess.ShouldBeTrue();
        db.StockBatches.Single().ItemId.ShouldBe(primary.Id);
        db.ClassBalances.Count().ShouldBe(1);
    }

    [Test]
    public void Command_IsRestrictedToAdministrators()
    {
        var attribute = typeof(MergeItemsCommand)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>()
            .ShouldHaveSingleItem();

        attribute.Roles.ShouldBe(Roles.Administrator);
    }

    [Test]
    public void Command_DoesNotRequireAnOpenClass()
    {
        typeof(MergeItemsCommand).GetInterfaces().ShouldNotContain(typeof(IRequiresOpenClass));
    }
}

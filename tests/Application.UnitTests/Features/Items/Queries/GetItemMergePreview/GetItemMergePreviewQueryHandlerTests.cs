using NUnit.Framework;
using Shouldly;
using skestock.Application.Common.Errors;
using skestock.Application.Common.Security;
using skestock.Application.Features.Items.Commands.MergeItems;
using skestock.Application.Features.Items.Commands.MergeItems.Mergers;
using skestock.Application.Features.Items.Queries.GetItemMergePreview;
using skestock.Domain.Constants;
using skestock.Domain.Entities.Items;
using skestock.Domain.Entities.Stock;
using skestock.Domain.Entities.SupplyLists;
using skestock.Application.UnitTests.Features.Items.Commands.MergeItems;

namespace skestock.Application.UnitTests.Features.Items.Queries.GetItemMergePreview;

public class GetItemMergePreviewQueryHandlerTests
{
    private static IItemReferenceMerger[] Mergers(MergeTestDbContext db) =>
    [
        new StockBatchItemMerger(db), new StockTransactionItemMerger(db), new GoodsReceiptImportLineItemMerger(db),
        new OrderListLineItemMerger(db), new ClassItemStockVisibilityItemMerger(db),
        new ItemLocationThresholdItemMerger(db), new SupplyListLineItemMerger(db),
        new DailyItemConsumptionItemMerger(db), new ClassBalanceItemMerger(db),
        new ItemPurchaseStatisticItemMerger(db)
    ];

    private static async Task<(MergeTestDbContext Db, Item Primary, Item Secondary)> SeedAsync(bool primaryActive = true)
    {
        var db = MergeTestData.NewContext();
        var primary = MergeTestData.NewItem("Pencil", primaryActive);
        var secondary = MergeTestData.NewItem("Pencil (dup)");
        db.Items.AddRange(primary, secondary);
        var listId = Guid.NewGuid();
        db.StockBatches.AddRange(new StockBatch { ItemId = secondary.Id }, new StockBatch { ItemId = secondary.Id });
        // Collision on SupplyListId -> counted as Combined.
        db.SupplyListLines.AddRange(
            new SupplyListLine { ItemId = primary.Id, SupplyListId = listId, Unit = "u", Quantity = 1 },
            new SupplyListLine { ItemId = secondary.Id, SupplyListId = listId, Unit = "u", Quantity = 2 });
        await db.SaveChangesAsync();
        return (db, primary, secondary);
    }

    [Test]
    public async Task Handle_ReportsNonZeroKindsAndMatchesWhatTheMergeChanges()
    {
        var (db, primary, secondary) = await SeedAsync();
        await using var _ = db;
        var mergers = Mergers(db);

        var result = await new GetItemMergePreviewQueryHandler(db, mergers)
            .Handle(new GetItemMergePreviewQuery(primary.Id, [primary.Id, secondary.Id]), default);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Impacts.Select(i => i.Kind)
            .ShouldBe([ItemMergeKind.StockBatches, ItemMergeKind.SupplyListLines], ignoreOrder: true);
        db.ChangeTracker.HasChanges().ShouldBeFalse();

        await new MergeItemsCommandHandler(db, mergers)
            .Handle(new MergeItemsCommand(primary.Id, [primary.Id, secondary.Id]), default);

        db.StockBatches.Count(b => b.ItemId == primary.Id).ShouldBe(
            result.Value.Impacts.Single(i => i.Kind == ItemMergeKind.StockBatches).Relinked);
        result.Value.Impacts.Single(i => i.Kind == ItemMergeKind.SupplyListLines).Combined.ShouldBe(1);
        db.SupplyListLines.Count().ShouldBe(1);
    }

    [Test]
    public async Task Handle_WithUnknownItem_ReturnsItemNotFound()
    {
        var (db, primary, _) = await SeedAsync();
        await using var _db = db;

        var result = await new GetItemMergePreviewQueryHandler(db, Mergers(db))
            .Handle(new GetItemMergePreviewQuery(primary.Id, [primary.Id, Guid.NewGuid()]), default);

        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(e => e is ItemErrors.ItemNotFound);
    }

    [Test]
    public async Task Handle_WithInactivePrimary_ReturnsMergePrimaryInactive()
    {
        var (db, primary, secondary) = await SeedAsync(primaryActive: false);
        await using var _ = db;

        var result = await new GetItemMergePreviewQueryHandler(db, Mergers(db))
            .Handle(new GetItemMergePreviewQuery(primary.Id, [primary.Id, secondary.Id]), default);

        result.Errors.ShouldContain(e => e is ItemErrors.MergePrimaryInactive);
    }

    [Test]
    public async Task Validator_RejectsSetsThatTheCommandRejects()
    {
        var id = Guid.NewGuid();
        var validator = new GetItemMergePreviewQueryValidator();

        (await validator.ValidateAsync(new GetItemMergePreviewQuery(id, []))).Errors
            .ShouldContain(e => e.ErrorCode == ValidationErrorCodes.Required);
        (await validator.ValidateAsync(new GetItemMergePreviewQuery(id, [id, id]))).Errors
            .ShouldContain(e => e.ErrorCode == ValidationErrorCodes.GreaterThanOrEqualTo);
        (await validator.ValidateAsync(new GetItemMergePreviewQuery(id, [Guid.NewGuid(), Guid.NewGuid()]))).Errors
            .ShouldContain(e => e.ErrorCode == ValidationErrorCodes.InvalidReference);
        (await validator.ValidateAsync(new GetItemMergePreviewQuery(id, [id, Guid.NewGuid()]))).IsValid.ShouldBeTrue();
    }

    [Test]
    public void Query_RequiresAdministrator()
    {
        typeof(GetItemMergePreviewQuery).GetCustomAttributes(typeof(AuthorizeAttribute), false)
            .Cast<AuthorizeAttribute>().ShouldContain(a => a.Roles == Roles.Administrator);
    }
}

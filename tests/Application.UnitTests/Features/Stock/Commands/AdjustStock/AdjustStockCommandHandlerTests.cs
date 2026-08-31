using Microsoft.EntityFrameworkCore;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.Stock.Commands.AdjustStock;
using skestock.Application.UnitTests.Features.GoodsReceipts.Commands.CreateGoodsReceipt;
using skestock.Domain.Entities;
using skestock.Domain.Enums;
using NUnit.Framework;
using Shouldly;

namespace skestock.Application.UnitTests.Features.Stock.Commands.AdjustStock;

public class AdjustStockCommandHandlerTests
{
    private static async Task<(GoodsReceiptTestDbContext Context, Item Item, Location Location, SchoolClass Class, UserProfile UserProfile)> CreateContextAsync()
    {
        var options = new DbContextOptionsBuilder<GoodsReceiptTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new GoodsReceiptTestDbContext(options);

        var category = new Category { Name = "Pantry" };
        context.Categories.Add(category);

        var item = new Item { Name = "Rice", Unit = "kg", MinThreshold = 10, IsPerishable = false, Category = category };
        context.Items.Add(item);

        var location = new Location { Name = "Main Storage", Type = "StorageRoom" };
        context.Locations.Add(location);

        var schoolClass = new SchoolClass
        {
            Name = "Fall 2026",
            StartDate = new DateOnly(2026, 9, 1),
            EndDate = new DateOnly(2026, 12, 20)
        };
        context.SchoolClasses.Add(schoolClass);

        // StockTransaction.UserId links directly to the caller's Identity/AspNetUsers id
        // (UserProfile.IdentityId is the FK's principal key), so no UserProfile.Id resolution
        // happens in the handler; the profile only needs to exist.
        var userProfile = new UserProfile { IdentityId = 42, FirstName = "Staff", LastName = "Member" };
        context.UserProfiles.Add(userProfile);

        await context.SaveChangesAsync(CancellationToken.None);

        return (context, item, location, schoolClass, userProfile);
    }

    private static StockBatch CreateBatch(Item item, Location location, SchoolClass schoolClass, int quantity, DateOnly? expiryDate)
    {
        return new StockBatch
        {
            ItemId = item.Id,
            LocationId = location.Id,
            ReceivedClassId = schoolClass.Id,
            Quantity = quantity,
            ExpiryDate = expiryDate,
            ReceivedDate = new DateOnly(2026, 1, 1),
            UnitPrice = 2.5m
        };
    }

    [Test]
    public async Task Handle_WithShortfallFromSingleBatch_ReducesBatchAndCreatesNegativeAdjustmentTransaction()
    {
        var (context, item, location, schoolClass, userProfile) = await CreateContextAsync();
        await using var _ = context;

        var batch = CreateBatch(item, location, schoolClass, quantity: 20, expiryDate: new DateOnly(2026, 6, 1));
        context.StockBatches.Add(batch);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new AdjustStockCommandHandler(context, new FakeUser(userProfile.IdentityId));
        var command = new AdjustStockCommand
        {
            ClassId = schoolClass.Id,
            ItemId = item.Id,
            LocationId = location.Id,
            ActualQuantity = 10,
            Reason = AdjustmentReason.Miscount
        };

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Quantity.ShouldBe(10);

        var updatedBatch = await context.StockBatches.SingleAsync(CancellationToken.None);
        updatedBatch.Quantity.ShouldBe(10);

        var transaction = await context.StockTransactions.SingleAsync(CancellationToken.None);
        transaction.Type.ShouldBe(StockTransactionType.Adjustment);
        transaction.QuantityChange.ShouldBe(-10);
        transaction.BatchId.ShouldBe(batch.Id);
        transaction.Reason.ShouldBe(nameof(AdjustmentReason.Miscount));
        transaction.UserId.ShouldBe(userProfile.IdentityId);
        transaction.ClassId.ShouldBe(schoolClass.Id);
    }

    [Test]
    public async Task Handle_WithShortfallSpanningMultipleBatches_ConsumesOldestExpiryFirst()
    {
        var (context, item, location, schoolClass, userProfile) = await CreateContextAsync();
        await using var _ = context;

        // Oldest expiry first: batch A (6 left, expires soonest) must be fully drained before
        // batch B (10 left, expires later) is touched at all.
        var batchA = CreateBatch(item, location, schoolClass, quantity: 6, expiryDate: new DateOnly(2026, 1, 15));
        var batchB = CreateBatch(item, location, schoolClass, quantity: 10, expiryDate: new DateOnly(2026, 2, 15));
        context.StockBatches.AddRange(batchA, batchB);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new AdjustStockCommandHandler(context, new FakeUser(userProfile.IdentityId));
        var command = new AdjustStockCommand
        {
            ClassId = schoolClass.Id,
            ItemId = item.Id,
            LocationId = location.Id,
            ActualQuantity = 6, // currentTotal (16) - 10 = 6
            Reason = AdjustmentReason.Damaged
        };

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Quantity.ShouldBe(6);

        var updatedBatchA = await context.StockBatches.SingleAsync(b => b.Id == batchA.Id, CancellationToken.None);
        var updatedBatchB = await context.StockBatches.SingleAsync(b => b.Id == batchB.Id, CancellationToken.None);
        updatedBatchA.Quantity.ShouldBe(0);
        updatedBatchB.Quantity.ShouldBe(6);

        var transactions = await context.StockTransactions.ToListAsync(CancellationToken.None);
        transactions.Count.ShouldBe(2);
        transactions.Single(t => t.BatchId == batchA.Id).QuantityChange.ShouldBe(-6);
        transactions.Single(t => t.BatchId == batchB.Id).QuantityChange.ShouldBe(-4);
        transactions.ShouldAllBe(t => t.Type == StockTransactionType.Adjustment && t.Reason == nameof(AdjustmentReason.Damaged));
    }

    [Test]
    public async Task Handle_WithBatchesWithAndWithoutExpiry_ConsumesExpiringBatchesBeforeNoExpiryBatch()
    {
        var (context, item, location, schoolClass, userProfile) = await CreateContextAsync();
        await using var _ = context;

        var noExpiryBatch = CreateBatch(item, location, schoolClass, quantity: 5, expiryDate: null);
        var expiringBatch = CreateBatch(item, location, schoolClass, quantity: 5, expiryDate: new DateOnly(2026, 3, 1));
        context.StockBatches.AddRange(noExpiryBatch, expiringBatch);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new AdjustStockCommandHandler(context, new FakeUser(userProfile.IdentityId));
        var command = new AdjustStockCommand
        {
            ClassId = schoolClass.Id,
            ItemId = item.Id,
            LocationId = location.Id,
            ActualQuantity = 7, // currentTotal (10) - 3 => drawn entirely from the batch that has an expiry
            Reason = AdjustmentReason.Expired
        };

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();

        var updatedExpiringBatch = await context.StockBatches.SingleAsync(b => b.Id == expiringBatch.Id, CancellationToken.None);
        var updatedNoExpiryBatch = await context.StockBatches.SingleAsync(b => b.Id == noExpiryBatch.Id, CancellationToken.None);
        updatedExpiringBatch.Quantity.ShouldBe(2);
        updatedNoExpiryBatch.Quantity.ShouldBe(5); // untouched - batches with no expiry go last
    }

    [Test]
    public async Task Handle_WithSurplus_CreatesNewUnattributedBatchAndPositiveAdjustmentTransaction()
    {
        var (context, item, location, schoolClass, userProfile) = await CreateContextAsync();
        await using var _ = context;

        var batch = CreateBatch(item, location, schoolClass, quantity: 5, expiryDate: new DateOnly(2026, 6, 1));
        context.StockBatches.Add(batch);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new AdjustStockCommandHandler(context, new FakeUser(userProfile.IdentityId));
        var command = new AdjustStockCommand
        {
            ClassId = schoolClass.Id,
            ItemId = item.Id,
            LocationId = location.Id,
            ActualQuantity = 10, // currentTotal (5) + 5 surplus
            Reason = AdjustmentReason.Found
        };

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Quantity.ShouldBe(10);

        (await context.StockBatches.CountAsync(CancellationToken.None)).ShouldBe(2);
        var surplusBatch = await context.StockBatches.SingleAsync(b => b.Id != batch.Id, CancellationToken.None);
        surplusBatch.Quantity.ShouldBe(5);
        surplusBatch.ExpiryDate.ShouldBeNull();
        surplusBatch.UnitPrice.ShouldBe(0);
        surplusBatch.GoodsReceiptId.ShouldBeNull();

        var surplusTransaction = await context.StockTransactions.SingleAsync(t => t.BatchId == surplusBatch.Id, CancellationToken.None);
        surplusTransaction.Type.ShouldBe(StockTransactionType.Adjustment);
        surplusTransaction.QuantityChange.ShouldBe(5);
        surplusTransaction.Reason.ShouldBe(nameof(AdjustmentReason.Found));
    }
}

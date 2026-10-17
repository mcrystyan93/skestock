using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;
using skestock.Application.Common.Errors;
using skestock.Application.Features.Stock.Commands.BulkAdjustStock;
using skestock.Application.UnitTests.Features.GoodsReceipts.Commands.CreateGoodsReceipt;
using skestock.Domain.Entities.Stock;
using skestock.Domain.Enums;
using skestock.Domain.Events.Stock;
using Line = skestock.Application.Features.Stock.Commands.BulkAdjustStock.BulkAdjustStockCommand.Line;

namespace skestock.Application.UnitTests.Features.Stock.Commands.BulkAdjustStock;


public class BulkAdjustStockCommandHandlerTests
{
    private static BulkAdjustStockCommand Command(BulkAdjustStockTestData d, params BulkAdjustStockCommand.Line[] lines) => new()
    {
        ClassId = d.Class.Id,
        Reason = AdjustmentReason.Miscount,
        Lines = lines
    };

    private static BulkAdjustStockCommandHandler Handler(BulkAdjustStockTestData d) =>
        new(d.Context, new FakeUser(d.User.IdentityId));

    [Test]
    public async Task Handle_WithShortfallAcrossBatches_ConsumesOldestExpiryFirst()
    {
        var d = await BulkAdjustStockTestData.CreateAsync();
        await using var _ = d.Context;
        var oldest = d.AddBatch(d.Rice, d.Pantry, 6, new DateOnly(2026, 3, 1));
        var newest = d.AddBatch(d.Rice, d.Pantry, 10, new DateOnly(2026, 9, 1));
        var noExpiry = d.AddBatch(d.Rice, d.Pantry, 4);
        await d.Context.SaveChangesAsync(CancellationToken.None);

        var result = await Handler(d).Handle(
            Command(d, new Line(d.Rice.Id, d.Pantry.Id, 8)), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        (await d.Context.StockBatches.FindAsync(oldest.Id))!.Quantity.ShouldBe(0);
        (await d.Context.StockBatches.FindAsync(newest.Id))!.Quantity.ShouldBe(4);
        (await d.Context.StockBatches.FindAsync(noExpiry.Id))!.Quantity.ShouldBe(4);
    }

    [Test]
    public async Task Handle_WithSurplus_CreatesUnattributedBatch()
    {
        var d = await BulkAdjustStockTestData.CreateAsync();
        await using var _ = d.Context;
        d.AddBatch(d.Rice, d.Pantry, 5, new DateOnly(2026, 3, 1));
        await d.Context.SaveChangesAsync(CancellationToken.None);

        var result = await Handler(d).Handle(
            Command(d, new Line(d.Rice.Id, d.Pantry.Id, 12)), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        var surplus = await d.Context.StockBatches.SingleAsync(b => b.GoodsReceiptId == null && b.ExpiryDate == null);
        surplus.Quantity.ShouldBe(7);
        var tx = await d.Context.StockTransactions.SingleAsync();
        tx.QuantityChange.ShouldBe(7);
        tx.Type.ShouldBe(StockTransactionType.Adjustment);
        tx.Reason.ShouldBe(nameof(AdjustmentReason.Miscount));
        tx.UserId.ShouldBe(d.User.IdentityId);
    }

    [Test]
    public async Task Handle_WhenCountEqualsCurrentTotal_SkipsTheLine()
    {
        var d = await BulkAdjustStockTestData.CreateAsync();
        await using var _ = d.Context;
        var batch = d.AddBatch(d.Rice, d.Pantry, 5);
        await d.Context.SaveChangesAsync(CancellationToken.None);

        var result = await Handler(d).Handle(
            Command(d, new Line(d.Rice.Id, d.Pantry.Id, 5)), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        (await d.Context.StockTransactions.CountAsync()).ShouldBe(0);
        batch.DomainEvents.ShouldBeEmpty();
    }

    [Test]
    public async Task Handle_WithTwoLocations_RaisesOneEventPerTouchedLocation()
    {
        var d = await BulkAdjustStockTestData.CreateAsync();
        await using var _ = d.Context;
        d.AddBatch(d.Rice, d.Pantry, 10);
        d.AddBatch(d.Flour, d.Pantry, 10);
        d.AddBatch(d.Rice, d.Cellar, 10);
        await d.Context.SaveChangesAsync(CancellationToken.None);

        var result = await Handler(d).Handle(
            Command(d,
                new Line(d.Rice.Id, d.Pantry.Id, 4),
                new Line(d.Flour.Id, d.Pantry.Id, 3),
                new Line(d.Rice.Id, d.Cellar.Id, 1)),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        var events = d.Context.StockBatches.Local
            .SelectMany(b => b.DomainEvents)
            .OfType<StockAdjustedEvent>()
            .ToList();
        events.Count.ShouldBe(2);
        events.Select(e => e.LocationId).ShouldBe([d.Pantry.Id, d.Cellar.Id], ignoreOrder: true);
    }

    [Test]
    public async Task Handle_WithShortfallTouchingTwoBatches_WritesOneTransactionPerTouchedBatch()
    {
        var d = await BulkAdjustStockTestData.CreateAsync();
        await using var _ = d.Context;
        d.AddBatch(d.Rice, d.Pantry, 3, new DateOnly(2026, 3, 1));
        d.AddBatch(d.Rice, d.Pantry, 10, new DateOnly(2026, 9, 1));
        d.AddBatch(d.Rice, d.Pantry, 10, new DateOnly(2026, 12, 1));
        await d.Context.SaveChangesAsync(CancellationToken.None);

        await Handler(d).Handle(Command(d, new Line(d.Rice.Id, d.Pantry.Id, 18)), CancellationToken.None);

        var txs = await d.Context.StockTransactions.ToListAsync();
        txs.Count.ShouldBe(2);
        txs.Select(t => t.QuantityChange).OrderBy(q => q).ShouldBe([-3, -2]);
    }

    [Test]
    public async Task Handle_WhenSaveHitsConcurrencyConflict_ReturnsTypedConflict()
    {
        var d = await BulkAdjustStockTestData.CreateAsync(o => new ConcurrencyThrowingDbContext(o));
        await using var _ = d.Context;
        d.AddBatch(d.Rice, d.Pantry, 10);
        await d.Context.SaveChangesAsync(CancellationToken.None);
        ((ConcurrencyThrowingDbContext)d.Context).ThrowOnNextSave = true;

        var result = await Handler(d).Handle(
            Command(d, new Line(d.Rice.Id, d.Pantry.Id, 2)), CancellationToken.None);

        result.IsFailed.ShouldBeTrue();
        var error = result.Errors.Single();
        error.Metadata[ErrorMetadataKeys.Code].ShouldBe(StockErrors.BulkConcurrencyConflict.ErrorCode);
        error.Metadata[ErrorMetadataKeys.StatusCode].ShouldBe(409);
    }
}

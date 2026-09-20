using Microsoft.EntityFrameworkCore;
using skestock.Application.Common.Errors;
using skestock.Application.Features.Stock.Commands.MoveStock;
using skestock.Application.UnitTests.Features.GoodsReceipts.Commands.CreateGoodsReceipt;
using skestock.Domain.Entities;
using skestock.Domain.Enums;
using skestock.Domain.Events.Stock;
using NUnit.Framework;
using Shouldly;

namespace skestock.Application.UnitTests.Features.Stock.Commands.MoveStock;

public class MoveStockCommandHandlerTests
{
    [Test]
    public async Task Handle_WithPartialSingleBatchMove_ReducesSourceAndCreatesPairedTransactions()
    {
        var fixture = await MoveStockTestData.CreateFixtureAsync();
        await using var _ = fixture.Context;
        var sourceBatch = MoveStockTestData.CreateBatch(
            fixture,
            quantity: 10,
            expiryDate: new DateOnly(2026, 10, 1),
            receivedDate: new DateOnly(2026, 1, 1),
            unitPrice: 2.5m,
            goodsReceiptId: Guid.NewGuid());
        fixture.Context.StockBatches.Add(sourceBatch);
        await fixture.Context.SaveChangesAsync(CancellationToken.None);

        var result = await CreateHandler(fixture).Handle(CreateCommand(fixture, 4), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();

        var batches = await fixture.Context.StockBatches
            .OrderBy(b => b.LocationId)
            .ToListAsync(CancellationToken.None);
        batches.Count.ShouldBe(2);
        var destinationBatch = batches.Single(b => b.LocationId == fixture.DestinationLocation.Id);
        destinationBatch.Quantity.ShouldBe(4);
        destinationBatch.ItemId.ShouldBe(sourceBatch.ItemId);
        destinationBatch.ReceivedClassId.ShouldBe(sourceBatch.ReceivedClassId);
        destinationBatch.ExpiryDate.ShouldBe(sourceBatch.ExpiryDate);
        destinationBatch.ReceivedDate.ShouldBe(sourceBatch.ReceivedDate);
        destinationBatch.UnitPrice.ShouldBe(sourceBatch.UnitPrice);
        destinationBatch.GoodsReceiptId.ShouldBe(sourceBatch.GoodsReceiptId);

        var transactions = await fixture.Context.StockTransactions.ToListAsync(CancellationToken.None);
        transactions.Count.ShouldBe(2);
        transactions.ShouldAllBe(t =>
            t.Type == StockTransactionType.Transfer
            && t.Reason == null
            && t.UserId == fixture.UserProfile.IdentityId
            && t.ClassId == fixture.Class.Id);
        transactions.Single(t => t.LocationId == fixture.SourceLocation.Id).QuantityChange.ShouldBe(-4);
        transactions.Single(t => t.LocationId == fixture.DestinationLocation.Id).QuantityChange.ShouldBe(4);

        var movedEvent = sourceBatch.DomainEvents.Single().ShouldBeOfType<StockMovedEvent>();
        movedEvent.ClassId.ShouldBe(fixture.Class.Id);
        movedEvent.ItemId.ShouldBe(fixture.Item.Id);
        movedEvent.SourceLocationId.ShouldBe(fixture.SourceLocation.Id);
        movedEvent.DestinationLocationId.ShouldBe(fixture.DestinationLocation.Id);
        movedEvent.Quantity.ShouldBe(4);
    }

    [Test]
    public async Task Handle_WithFullMoveDepletesSourceBatch()
    {
        var fixture = await MoveStockTestData.CreateFixtureAsync();
        await using var _ = fixture.Context;
        fixture.Context.StockBatches.Add(MoveStockTestData.CreateBatch(
            fixture,
            quantity: 5,
            expiryDate: null,
            receivedDate: new DateOnly(2026, 1, 1),
            unitPrice: 1));
        await fixture.Context.SaveChangesAsync(CancellationToken.None);

        var result = await CreateHandler(fixture).Handle(CreateCommand(fixture, 5), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        (await fixture.Context.StockBatches
                .Where(b => b.LocationId == fixture.SourceLocation.Id)
                .Select(b => b.Quantity)
                .SingleAsync(CancellationToken.None))
            .ShouldBe(0);
        (await fixture.Context.StockBatches
                .Where(b => b.LocationId == fixture.DestinationLocation.Id)
                .Select(b => b.Quantity)
                .SingleAsync(CancellationToken.None))
            .ShouldBe(5);
    }

    [Test]
    public async Task Handle_WithMultipleBatches_ConsumesFifoAndPreservesMetadataPerPortion()
    {
        var fixture = await MoveStockTestData.CreateFixtureAsync();
        await using var _ = fixture.Context;
        var firstReceiptId = Guid.NewGuid();
        var secondReceiptId = Guid.NewGuid();
        var firstBatch = MoveStockTestData.CreateBatch(
            fixture,
            quantity: 4,
            expiryDate: new DateOnly(2026, 1, 10),
            receivedDate: new DateOnly(2025, 12, 1),
            unitPrice: 1.25m,
            goodsReceiptId: firstReceiptId);
        var secondBatch = MoveStockTestData.CreateBatch(
            fixture,
            quantity: 6,
            expiryDate: new DateOnly(2026, 2, 10),
            receivedDate: new DateOnly(2026, 1, 1),
            unitPrice: 2.75m,
            goodsReceiptId: secondReceiptId);
        fixture.Context.StockBatches.AddRange(firstBatch, secondBatch);
        await fixture.Context.SaveChangesAsync(CancellationToken.None);

        var result = await CreateHandler(fixture).Handle(CreateCommand(fixture, 7), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        (await fixture.Context.StockBatches
                .SingleAsync(b => b.Id == firstBatch.Id, CancellationToken.None))
            .Quantity.ShouldBe(0);
        (await fixture.Context.StockBatches
                .SingleAsync(b => b.Id == secondBatch.Id, CancellationToken.None))
            .Quantity.ShouldBe(3);

        var destinationBatches = await fixture.Context.StockBatches
            .Where(b => b.LocationId == fixture.DestinationLocation.Id)
            .OrderBy(b => b.ExpiryDate)
            .ToListAsync(CancellationToken.None);
        destinationBatches.Count.ShouldBe(2);
        destinationBatches[0].Quantity.ShouldBe(4);
        destinationBatches[0].ExpiryDate.ShouldBe(firstBatch.ExpiryDate);
        destinationBatches[0].ReceivedDate.ShouldBe(firstBatch.ReceivedDate);
        destinationBatches[0].UnitPrice.ShouldBe(firstBatch.UnitPrice);
        destinationBatches[0].GoodsReceiptId.ShouldBe(firstReceiptId);
        destinationBatches[1].Quantity.ShouldBe(3);
        destinationBatches[1].ExpiryDate.ShouldBe(secondBatch.ExpiryDate);
        destinationBatches[1].ReceivedDate.ShouldBe(secondBatch.ReceivedDate);
        destinationBatches[1].UnitPrice.ShouldBe(secondBatch.UnitPrice);
        destinationBatches[1].GoodsReceiptId.ShouldBe(secondReceiptId);

        var transactions = await fixture.Context.StockTransactions.ToListAsync(CancellationToken.None);
        transactions.Count.ShouldBe(4);
        transactions.Count(t => t.LocationId == fixture.SourceLocation.Id && t.QuantityChange == -4).ShouldBe(1);
        transactions.Count(t => t.LocationId == fixture.SourceLocation.Id && t.QuantityChange == -3).ShouldBe(1);
        transactions.Count(t => t.LocationId == fixture.DestinationLocation.Id && t.QuantityChange == 4).ShouldBe(1);
        transactions.Count(t => t.LocationId == fixture.DestinationLocation.Id && t.QuantityChange == 3).ShouldBe(1);
    }

    [Test]
    public async Task Handle_WithExistingDestinationStock_CreatesDistinctDestinationBatch()
    {
        var fixture = await MoveStockTestData.CreateFixtureAsync();
        await using var _ = fixture.Context;
        var sourceBatch = MoveStockTestData.CreateBatch(
            fixture,
            quantity: 10,
            expiryDate: new DateOnly(2026, 1, 10),
            receivedDate: new DateOnly(2026, 1, 1),
            unitPrice: 1m);
        var existingDestinationBatch = new StockBatch
        {
            ItemId = fixture.Item.Id,
            LocationId = fixture.DestinationLocation.Id,
            ReceivedClassId = fixture.Class.Id,
            Quantity = 8,
            ExpiryDate = new DateOnly(2026, 3, 10),
            ReceivedDate = new DateOnly(2026, 2, 1),
            UnitPrice = 3m
        };
        fixture.Context.StockBatches.AddRange(sourceBatch, existingDestinationBatch);
        await fixture.Context.SaveChangesAsync(CancellationToken.None);

        var result = await CreateHandler(fixture).Handle(CreateCommand(fixture, 3), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        var destinationBatches = await fixture.Context.StockBatches
            .Where(b => b.LocationId == fixture.DestinationLocation.Id)
            .ToListAsync(CancellationToken.None);
        destinationBatches.Count.ShouldBe(2);
        destinationBatches.Single(b => b.Id == existingDestinationBatch.Id).Quantity.ShouldBe(8);
        destinationBatches.Single(b => b.Id != existingDestinationBatch.Id).Quantity.ShouldBe(3);
    }

    [Test]
    public async Task Handle_WhenQuantityExceedsSource_ReturnsFailureWithoutPersistence()
    {
        var fixture = await MoveStockTestData.CreateFixtureAsync();
        await using var _ = fixture.Context;
        var sourceBatch = MoveStockTestData.CreateBatch(
            fixture,
            quantity: 2,
            expiryDate: null,
            receivedDate: new DateOnly(2026, 1, 1),
            unitPrice: 1m);
        fixture.Context.StockBatches.Add(sourceBatch);
        await fixture.Context.SaveChangesAsync(CancellationToken.None);

        var result = await CreateHandler(fixture).Handle(CreateCommand(fixture, 3), CancellationToken.None);

        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(error => error is StockErrors.InsufficientQuantity);
        (await fixture.Context.StockBatches.CountAsync(CancellationToken.None)).ShouldBe(1);
        (await fixture.Context.StockBatches.SingleAsync(CancellationToken.None)).Quantity.ShouldBe(2);
        (await fixture.Context.StockTransactions.CountAsync(CancellationToken.None)).ShouldBe(0);
    }

    [Test]
    public async Task Handle_WhenLocationsAreTheSame_ReturnsFailureWithoutPersistence()
    {
        var fixture = await MoveStockTestData.CreateFixtureAsync();
        await using var _ = fixture.Context;
        var sourceBatch = MoveStockTestData.CreateBatch(
            fixture,
            quantity: 2,
            expiryDate: null,
            receivedDate: new DateOnly(2026, 1, 1),
            unitPrice: 1m);
        fixture.Context.StockBatches.Add(sourceBatch);
        await fixture.Context.SaveChangesAsync(CancellationToken.None);

        var result = await CreateHandler(fixture).Handle(
            new MoveStockCommand
            {
                ClassId = fixture.Class.Id,
                ItemId = fixture.Item.Id,
                SourceLocationId = fixture.SourceLocation.Id,
                DestinationLocationId = fixture.SourceLocation.Id,
                Quantity = 1
            },
            CancellationToken.None);

        result.IsFailed.ShouldBeTrue();
        (await fixture.Context.StockBatches.CountAsync(CancellationToken.None)).ShouldBe(1);
        (await fixture.Context.StockBatches.SingleAsync(CancellationToken.None)).Quantity.ShouldBe(2);
        (await fixture.Context.StockTransactions.CountAsync(CancellationToken.None)).ShouldBe(0);
    }

    [Test]
    public async Task Handle_WhenSaveHitsConcurrencyConflict_ReturnsTypedConflict()
    {
        var options = new DbContextOptionsBuilder<GoodsReceiptTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new ConcurrencyThrowingDbContext(options);

        var category = new Category { Name = "Pantry" };
        var item = new Item { Name = "Rice", Unit = "kg", MinThreshold = 10, IsPerishable = true, Category = category };
        var sourceLocation = new Location { Name = "Main Storage", Type = "StorageRoom" };
        var destinationLocation = new Location { Name = "Classroom", Type = "Classroom" };
        var schoolClass = new SchoolClass
        {
            Name = "Fall 2026",
            StartDate = new DateOnly(2026, 9, 1),
            EndDate = new DateOnly(2026, 12, 20)
        };
        var userProfile = new UserProfile { IdentityId = Guid.NewGuid(), FirstName = "Staff", LastName = "Member" };
        context.Categories.Add(category);
        context.Items.Add(item);
        context.Locations.AddRange(sourceLocation, destinationLocation);
        context.SchoolClasses.Add(schoolClass);
        context.UserProfiles.Add(userProfile);
        context.StockBatches.Add(new StockBatch
        {
            ItemId = item.Id,
            LocationId = sourceLocation.Id,
            ReceivedClassId = schoolClass.Id,
            Quantity = 10,
            ExpiryDate = new DateOnly(2026, 10, 1),
            ReceivedDate = new DateOnly(2026, 1, 1),
            UnitPrice = 2.5m
        });
        await context.SaveChangesAsync(CancellationToken.None);

        var fixture = new MoveStockFixture(context, item, sourceLocation, destinationLocation, schoolClass, userProfile);

        // Arm the simulated rowversion conflict for the handler's save only.
        context.ThrowOnNextSave = true;

        var result = await CreateHandler(fixture).Handle(CreateCommand(fixture, 4), CancellationToken.None);

        result.IsFailed.ShouldBeTrue();
        var error = result.Errors.Single();
        error.Metadata[ErrorMetadataKeys.Code].ShouldBe(StockErrors.ConcurrencyConflict.ErrorCode);
        error.Metadata[ErrorMetadataKeys.StatusCode].ShouldBe(409);
    }

    private static MoveStockCommandHandler CreateHandler(MoveStockFixture fixture) =>
        new(fixture.Context, new MoveStockFakeUser(fixture.UserProfile.IdentityId));

    private static MoveStockCommand CreateCommand(MoveStockFixture fixture, int quantity) =>
        new()
        {
            ClassId = fixture.Class.Id,
            ItemId = fixture.Item.Id,
            SourceLocationId = fixture.SourceLocation.Id,
            DestinationLocationId = fixture.DestinationLocation.Id,
            Quantity = quantity
        };
}

using Microsoft.EntityFrameworkCore;
using skestock.Application.Common.Errors;
using skestock.Application.Features.Stock.Commands.RemoveExpiredStock;
using skestock.Application.UnitTests.Features.GoodsReceipts.Commands.CreateGoodsReceipt;
using skestock.Domain.Entities;
using skestock.Domain.Enums;
using skestock.Domain.Events.Stock;
using NUnit.Framework;
using Shouldly;

namespace skestock.Application.UnitTests.Features.Stock.Commands.RemoveExpiredStock;

public class RemoveExpiredStockCommandHandlerTests
{
    [Test]
    public async Task Handle_RemovesOnlyExpiredBatchesAndAuditsEachRemoval()
    {
        var (context, item, location, schoolClass, userProfile) = await CreateContextAsync();
        await using var _ = context;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var expiredBatch = CreateBatch(item, location, schoolClass, 5, today.AddDays(-1));
        var secondExpiredBatch = CreateBatch(item, location, schoolClass, 2, today.AddDays(-10));
        var validBatch = CreateBatch(item, location, schoolClass, 8, today.AddDays(10));
        context.StockBatches.AddRange(expiredBatch, secondExpiredBatch, validBatch);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new RemoveExpiredStockCommandHandler(context, new FakeUser(userProfile.IdentityId));
        var result = await handler.Handle(new RemoveExpiredStockCommand
        {
            ClassId = schoolClass.Id,
            ItemId = item.Id,
            LocationId = location.Id
        }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        (await context.StockBatches.SingleAsync(b => b.Id == expiredBatch.Id)).Quantity.ShouldBe(0);
        (await context.StockBatches.SingleAsync(b => b.Id == secondExpiredBatch.Id)).Quantity.ShouldBe(0);
        (await context.StockBatches.SingleAsync(b => b.Id == validBatch.Id)).Quantity.ShouldBe(8);

        var transactions = await context.StockTransactions.ToListAsync();
        transactions.Count.ShouldBe(2);
        transactions.ShouldAllBe(transaction =>
            transaction.QuantityChange < 0
            && transaction.Reason == nameof(AdjustmentReason.Expired)
            && transaction.Type == StockTransactionType.Adjustment
            && transaction.UserId == userProfile.IdentityId);
        transactions.Select(transaction => transaction.BatchId)
            .ToHashSet()
            .ShouldBe(new HashSet<Guid?> { expiredBatch.Id, secondExpiredBatch.Id });

        secondExpiredBatch.DomainEvents.Single().ShouldBeOfType<StockAdjustedEvent>();
    }

    [Test]
    public async Task Handle_WhenNoExpiredStockExists_ReturnsTypedConflictAndDoesNotChangeStock()
    {
        var (context, item, location, schoolClass, userProfile) = await CreateContextAsync();
        await using var _ = context;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var validBatch = CreateBatch(item, location, schoolClass, 8, today.AddDays(1));
        context.StockBatches.Add(validBatch);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new RemoveExpiredStockCommandHandler(context, new FakeUser(userProfile.IdentityId));
        var result = await handler.Handle(new RemoveExpiredStockCommand
        {
            ClassId = schoolClass.Id,
            ItemId = item.Id,
            LocationId = location.Id
        }, CancellationToken.None);

        result.IsFailed.ShouldBeTrue();
        result.Errors.Single().Metadata[ErrorMetadataKeys.Code]
            .ShouldBe(StockErrors.NoExpiredQuantity.ErrorCode);
        (await context.StockBatches.SingleAsync()).Quantity.ShouldBe(8);
        (await context.StockTransactions.CountAsync()).ShouldBe(0);
    }

    [Test]
    public async Task Handle_WhenSaveHitsConcurrencyConflict_ReturnsTypedConflict()
    {
        var options = new DbContextOptionsBuilder<GoodsReceiptTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new ConcurrencyThrowingDbContext(options);

        var category = new Category { Name = "Pantry" };
        var item = new Item { Name = "Milk", Unit = "buc", MinThreshold = 2, IsPerishable = true, Category = category };
        var location = new Location { Name = "Main Storage", Type = "StorageRoom" };
        var schoolClass = new SchoolClass
        {
            Name = "Fall 2026",
            StartDate = new DateOnly(2026, 9, 1),
            EndDate = new DateOnly(2026, 12, 20)
        };
        var userProfile = new UserProfile { IdentityId = Guid.NewGuid(), FirstName = "Staff", LastName = "Member" };
        context.Categories.Add(category);
        context.Items.Add(item);
        context.Locations.Add(location);
        context.SchoolClasses.Add(schoolClass);
        context.UserProfiles.Add(userProfile);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        context.StockBatches.Add(CreateBatch(item, location, schoolClass, 5, today.AddDays(-1)));
        await context.SaveChangesAsync(CancellationToken.None);

        // Arm the simulated rowversion conflict for the handler's save only.
        context.ThrowOnNextSave = true;

        var handler = new RemoveExpiredStockCommandHandler(context, new FakeUser(userProfile.IdentityId));
        var result = await handler.Handle(new RemoveExpiredStockCommand
        {
            ClassId = schoolClass.Id,
            ItemId = item.Id,
            LocationId = location.Id
        }, CancellationToken.None);

        result.IsFailed.ShouldBeTrue();
        var error = result.Errors.Single();
        error.Metadata[ErrorMetadataKeys.Code].ShouldBe(StockErrors.ConcurrencyConflict.ErrorCode);
        error.Metadata[ErrorMetadataKeys.StatusCode].ShouldBe(409);
    }

    private static async Task<(GoodsReceiptTestDbContext Context, Item Item, Location Location, SchoolClass Class, UserProfile UserProfile)> CreateContextAsync()
    {
        var options = new DbContextOptionsBuilder<GoodsReceiptTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new GoodsReceiptTestDbContext(options);
        var category = new Category { Name = "Pantry" };
        var item = new Item
        {
            Name = "Milk",
            Unit = "buc",
            MinThreshold = 2,
            IsPerishable = true,
            Category = category
        };
        var location = new Location { Name = "Main Storage", Type = "StorageRoom" };
        var schoolClass = new SchoolClass
        {
            Name = "Fall 2026",
            StartDate = new DateOnly(2026, 9, 1),
            EndDate = new DateOnly(2026, 12, 20)
        };
        var userProfile = new UserProfile
        {
            IdentityId = Guid.NewGuid(),
            FirstName = "Staff",
            LastName = "Member"
        };

        context.Categories.Add(category);
        context.Items.Add(item);
        context.Locations.Add(location);
        context.SchoolClasses.Add(schoolClass);
        context.UserProfiles.Add(userProfile);
        await context.SaveChangesAsync(CancellationToken.None);
        return (context, item, location, schoolClass, userProfile);
    }

    private static StockBatch CreateBatch(
        Item item,
        Location location,
        SchoolClass schoolClass,
        int quantity,
        DateOnly expiryDate)
    {
        return new StockBatch
        {
            ItemId = item.Id,
            LocationId = location.Id,
            ReceivedClassId = schoolClass.Id,
            Quantity = quantity,
            ExpiryDate = expiryDate,
            ReceivedDate = expiryDate.AddDays(-10),
            UnitPrice = 2.5m
        };
    }
}

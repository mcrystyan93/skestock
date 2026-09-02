using Microsoft.EntityFrameworkCore;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.StockBatches.Commands.CreateStockBatch;
using skestock.Domain.Entities;
using skestock.Domain.Enums;
using NUnit.Framework;
using Shouldly;

namespace skestock.Application.UnitTests.Features.StockBatches.Commands.CreateStockBatch;

public class FakeUser(Guid? id) : IUser
{
    public Guid? Id { get; } = id;
    public List<string>? Roles { get; } = [];
}

public class CreateStockBatchCommandHandlerTests
{
    private static async Task<(CreateStockBatchTestDbContext Context, Item Item, Item PerishableItem, Location Location, SchoolClass Class, UserProfile UserProfile)> CreateContextAsync()
    {
        var options = new DbContextOptionsBuilder<CreateStockBatchTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new CreateStockBatchTestDbContext(options);

        var category = new Category { Name = "Pantry" };
        context.Categories.Add(category);

        var item = new Item { Name = "Rice", Unit = "kg", MinThreshold = 10, IsPerishable = false, Category = category };
        var perishableItem = new Item { Name = "Milk", Unit = "L", MinThreshold = 5, IsPerishable = true, Category = category };
        context.Items.AddRange(item, perishableItem);

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
        // (UserProfile.IdentityId is the FK's principal key - see StockTransactionConfiguration),
        // so no UserProfile.Id resolution happens in the handler; the profile only needs to exist.
        var userProfile = new UserProfile { IdentityId = Guid.NewGuid(), FirstName = "Staff", LastName = "Member" };
        context.UserProfiles.Add(userProfile);

        await context.SaveChangesAsync(CancellationToken.None);

        return (context, item, perishableItem, location, schoolClass, userProfile);
    }

    [Test]
    public async Task Handle_WithValidCommand_CreatesBatchAndOrderTransaction()
    {
        var (context, item, _, location, schoolClass, userProfile) = await CreateContextAsync();
        await using var _ = context;
        var handler = new CreateStockBatchCommandHandler(context, new FakeUser(userProfile.IdentityId));

        var command = new CreateStockBatchCommand
        {
            ItemId = item.Id,
            LocationId = location.Id,
            ReceivedClassId = schoolClass.Id,
            Quantity = 15,
            ReceivedDate = new DateOnly(2026, 9, 5),
            UnitPrice = 3.5m
        };

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ItemId.ShouldBe(item.Id);
        result.Value.ItemName.ShouldBe("Rice");
        result.Value.LocationId.ShouldBe(location.Id);
        result.Value.LocationName.ShouldBe("Main Storage");
        result.Value.Quantity.ShouldBe(15);
        result.Value.UnitPrice.ShouldBe(3.5m);
        result.Value.LineTotal.ShouldBe(52.5m);
        result.Value.GoodsReceiptId.ShouldBeNull();

        var batch = await context.StockBatches.SingleAsync(CancellationToken.None);
        batch.Quantity.ShouldBe(15);
        batch.ItemId.ShouldBe(item.Id);
        batch.LocationId.ShouldBe(location.Id);
        batch.ReceivedClassId.ShouldBe(schoolClass.Id);
        batch.GoodsReceiptId.ShouldBeNull();
        batch.ReceivedDate.ShouldBe(new DateOnly(2026, 9, 5));

        var transaction = await context.StockTransactions.SingleAsync(CancellationToken.None);
        transaction.Type.ShouldBe(StockTransactionType.Order);
        transaction.QuantityChange.ShouldBe(15);
        transaction.UserId.ShouldBe(userProfile.IdentityId);
        transaction.ClassId.ShouldBe(schoolClass.Id);
        transaction.GoodsReceiptId.ShouldBeNull();
        transaction.BatchId.ShouldBe(batch.Id);
    }

    [Test]
    public async Task Handle_WithPerishableItemAndExpiryDate_PersistsExpiryDate()
    {
        var (context, _, perishableItem, location, schoolClass, userProfile) = await CreateContextAsync();
        await using var _ = context;
        var handler = new CreateStockBatchCommandHandler(context, new FakeUser(userProfile.IdentityId));

        var command = new CreateStockBatchCommand
        {
            ItemId = perishableItem.Id,
            LocationId = location.Id,
            ReceivedClassId = schoolClass.Id,
            Quantity = 8,
            ExpiryDate = new DateOnly(2026, 10, 15),
            ReceivedDate = new DateOnly(2026, 9, 5),
            UnitPrice = 6m
        };

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ExpiryDate.ShouldBe(new DateOnly(2026, 10, 15));

        var batch = await context.StockBatches.SingleAsync(CancellationToken.None);
        batch.ExpiryDate.ShouldBe(new DateOnly(2026, 10, 15));
    }
}

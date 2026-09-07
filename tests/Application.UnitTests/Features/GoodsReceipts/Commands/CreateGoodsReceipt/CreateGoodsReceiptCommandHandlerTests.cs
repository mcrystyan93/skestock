using Microsoft.EntityFrameworkCore;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.GoodsReceipts.Commands.CreateGoodsReceipt;
using skestock.Domain.Entities;
using skestock.Domain.Enums;
using NUnit.Framework;
using Shouldly;

namespace skestock.Application.UnitTests.Features.GoodsReceipts.Commands.CreateGoodsReceipt;

public class FakeUser(Guid? id) : IUser
{
    public Guid? Id { get; } = id;
    public List<string>? Roles { get; } = [];
}

public class CreateGoodsReceiptCommandHandlerTests
{
    private static async Task<(GoodsReceiptTestDbContext Context, Item Item, Item PerishableItem, Location Location, SchoolClass Class, UserProfile UserProfile)> CreateContextAsync()
    {
        var options = new DbContextOptionsBuilder<GoodsReceiptTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new GoodsReceiptTestDbContext(options);

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
    public async Task Handle_WithSingleLine_CreatesOneBatchAndOneOrderTransaction()
    {
        var (context, item, _, location, schoolClass, userProfile) = await CreateContextAsync();
        await using var _ = context;
        var handler = new CreateGoodsReceiptCommandHandler(context, new FakeUser(userProfile.IdentityId));

        var command = new CreateGoodsReceiptCommand
        {
            ClassId = schoolClass.Id,
            Note = "Weekly delivery",
            Lines = [new CreateGoodsReceiptLine { ItemId = item.Id, LocationId = location.Id, Quantity = 25 }]
        };

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Lines.Count.ShouldBe(1);
        result.Value.Lines[0].Quantity.ShouldBe(25);
        result.Value.Lines[0].ItemName.ShouldBe("Rice");
        result.Value.Lines[0].LocationName.ShouldBe("Main Storage");

        var batch = await context.StockBatches.SingleAsync(CancellationToken.None);
        batch.Quantity.ShouldBe(25);
        batch.ItemId.ShouldBe(item.Id);
        batch.LocationId.ShouldBe(location.Id);
        batch.ReceivedClassId.ShouldBe(schoolClass.Id);
        batch.GoodsReceiptId.ShouldBe(result.Value.Id);

        var transaction = await context.StockTransactions.SingleAsync(CancellationToken.None);
        transaction.Type.ShouldBe(StockTransactionType.Order);
        transaction.QuantityChange.ShouldBe(25);
        transaction.UserId.ShouldBe(userProfile.IdentityId);
        transaction.ClassId.ShouldBe(schoolClass.Id);
        transaction.GoodsReceiptId.ShouldBe(result.Value.Id);
        transaction.BatchId.ShouldBe(batch.Id);
    }

    [Test]
    public async Task Handle_WithMultipleLines_CreatesOneBatchAndTransactionPerLine()
    {
        var (context, item, perishableItem, location, schoolClass, userProfile) = await CreateContextAsync();
        await using var _ = context;
        var handler = new CreateGoodsReceiptCommandHandler(context, new FakeUser(userProfile.IdentityId));

        var command = new CreateGoodsReceiptCommand
        {
            ClassId = schoolClass.Id,
            Note = "Bulk delivery",
            Lines =
            [
                new CreateGoodsReceiptLine { ItemId = item.Id, LocationId = location.Id, Quantity = 10 },
                new CreateGoodsReceiptLine { ItemId = perishableItem.Id, LocationId = location.Id, Quantity = 5, ExpiryDate = new DateOnly(2026, 10, 1) }
            ]
        };

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Lines.Count.ShouldBe(2);

        (await context.StockBatches.CountAsync(CancellationToken.None)).ShouldBe(2);
        (await context.StockTransactions.CountAsync(CancellationToken.None)).ShouldBe(2);

        var perishableBatch = await context.StockBatches.SingleAsync(b => b.ItemId == perishableItem.Id, CancellationToken.None);
        perishableBatch.ExpiryDate.ShouldBe(new DateOnly(2026, 10, 1));

        // All lines are tagged with the same receipt and class, as required by the flow.
        var receiptIds = await context.StockBatches.Select(b => b.GoodsReceiptId).Distinct().ToListAsync(CancellationToken.None);
        receiptIds.ShouldBe([result.Value.Id]);

        var classIds = await context.StockTransactions.Select(t => t.ClassId).Distinct().ToListAsync(CancellationToken.None);
        classIds.ShouldBe([schoolClass.Id]);
    }

    [Test]
    public async Task Handle_TrimsSupplierReferenceAndNoteAndPersistsHeader()
    {
        var (context, item, _, location, schoolClass, userProfile) = await CreateContextAsync();
        await using var _ = context;
        var handler = new CreateGoodsReceiptCommandHandler(context, new FakeUser(userProfile.IdentityId));

        var command = new CreateGoodsReceiptCommand
        {
            ClassId = schoolClass.Id,
            SupplierReference = "  PO-1001  ",
            Note = "  Delivered by supplier van  ",
            Lines = [new CreateGoodsReceiptLine { ItemId = item.Id, LocationId = location.Id, Quantity = 3 }]
        };

        var result = await handler.Handle(command, CancellationToken.None);

        result.Value.SupplierReference.ShouldBe("PO-1001");
        result.Value.Note.ShouldBe("Delivered by supplier van");
        result.Value.ClassName.ShouldBe("Fall 2026");
    }

    [Test]
    public async Task Handle_PerishableItemWithShelfLifeAndNoExpiry_DerivesExpiryFromReceivedDate()
    {
        var (context, _, _, location, schoolClass, userProfile) = await CreateContextAsync();
        await using var _ = context;

        var categoryId = await context.Categories.Select(c => c.Id).FirstAsync(CancellationToken.None);
        var shelfLifeItem = new Item { Name = "Yogurt", Unit = "cup", IsPerishable = true, ShelfLifeDays = 7, CategoryId = categoryId };
        context.Items.Add(shelfLifeItem);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new CreateGoodsReceiptCommandHandler(context, new FakeUser(userProfile.IdentityId));

        var command = new CreateGoodsReceiptCommand
        {
            ClassId = schoolClass.Id,
            Note = "Fresh delivery",
            Lines = [new CreateGoodsReceiptLine { ItemId = shelfLifeItem.Id, LocationId = location.Id, Quantity = 4 }]
        };

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();

        var batch = await context.StockBatches.SingleAsync(b => b.ItemId == shelfLifeItem.Id, CancellationToken.None);
        batch.ExpiryDate.ShouldBe(batch.ReceivedDate.AddDays(7));
    }

    [Test]
    public async Task Handle_PerishableItemWithShelfLifeButExplicitExpiry_PreservesSuppliedExpiry()
    {
        var (context, _, _, location, schoolClass, userProfile) = await CreateContextAsync();
        await using var _ = context;

        var categoryId = await context.Categories.Select(c => c.Id).FirstAsync(CancellationToken.None);
        var shelfLifeItem = new Item { Name = "Cheese", Unit = "kg", IsPerishable = true, ShelfLifeDays = 30, CategoryId = categoryId };
        context.Items.Add(shelfLifeItem);
        await context.SaveChangesAsync(CancellationToken.None);

        var explicitExpiry = new DateOnly(2027, 1, 15);
        var handler = new CreateGoodsReceiptCommandHandler(context, new FakeUser(userProfile.IdentityId));

        var command = new CreateGoodsReceiptCommand
        {
            ClassId = schoolClass.Id,
            Note = "Aged delivery",
            Lines = [new CreateGoodsReceiptLine { ItemId = shelfLifeItem.Id, LocationId = location.Id, Quantity = 2, ExpiryDate = explicitExpiry }]
        };

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();

        var batch = await context.StockBatches.SingleAsync(b => b.ItemId == shelfLifeItem.Id, CancellationToken.None);
        batch.ExpiryDate.ShouldBe(explicitExpiry);
    }

    [Test]
    public async Task Handle_NonPerishableItemWithShelfLife_DoesNotDeriveExpiry()
    {
        var (context, _, _, location, schoolClass, userProfile) = await CreateContextAsync();
        await using var _ = context;

        var categoryId = await context.Categories.Select(c => c.Id).FirstAsync(CancellationToken.None);
        var nonPerishable = new Item { Name = "Flour", Unit = "kg", IsPerishable = false, ShelfLifeDays = 90, CategoryId = categoryId };
        context.Items.Add(nonPerishable);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new CreateGoodsReceiptCommandHandler(context, new FakeUser(userProfile.IdentityId));

        var command = new CreateGoodsReceiptCommand
        {
            ClassId = schoolClass.Id,
            Note = "Dry goods",
            Lines = [new CreateGoodsReceiptLine { ItemId = nonPerishable.Id, LocationId = location.Id, Quantity = 20 }]
        };

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();

        var batch = await context.StockBatches.SingleAsync(b => b.ItemId == nonPerishable.Id, CancellationToken.None);
        batch.ExpiryDate.ShouldBeNull();
    }
}

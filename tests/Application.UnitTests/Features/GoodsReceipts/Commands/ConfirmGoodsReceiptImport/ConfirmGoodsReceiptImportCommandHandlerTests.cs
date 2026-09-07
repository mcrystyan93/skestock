using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;
using skestock.Application.Features.GoodsReceipts.Commands.ConfirmGoodsReceiptImport;
using skestock.Application.UnitTests.Features.GoodsReceipts.Commands.CreateGoodsReceipt;
using skestock.Domain.Entities;
using skestock.Domain.Enums;

namespace skestock.Application.UnitTests.Features.GoodsReceipts.Commands.ConfirmGoodsReceiptImport;

public class ConfirmGoodsReceiptImportCommandHandlerTests
{
    private static async Task<(ConfirmImportTestDbContext Context, Item Item, Category Category, Location Location, SchoolClass Class, UserProfile UserProfile)> CreateContextAsync()
    {
        var options = new DbContextOptionsBuilder<ConfirmImportTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new ConfirmImportTestDbContext(options);

        var category = new Category { Name = "Pantry" };
        context.Categories.Add(category);

        var item = new Item { Sku = "SKU-1", Name = "Rice", Unit = "kg", IsPerishable = false, Category = category };
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

        var userProfile = new UserProfile { IdentityId = Guid.NewGuid(), FirstName = "Staff", LastName = "Member" };
        context.UserProfiles.Add(userProfile);

        await context.SaveChangesAsync(CancellationToken.None);

        return (context, item, category, location, schoolClass, userProfile);
    }

    private static async Task<GoodsReceiptImport> AddPendingImportAsync(ConfirmImportTestDbContext context, Guid classId)
    {
        var import = GoodsReceiptImport.Create(classId, Guid.NewGuid(), Guid.NewGuid(), "blob/path.pdf");
        import.Status = GoodsReceiptImportStatus.PendingReview;
        context.GoodsReceiptImports.Add(import);
        await context.SaveChangesAsync(CancellationToken.None);
        return import;
    }

    [Test]
    public async Task Handle_WithExistingItem_CreatesReceiptBatchAndTransaction()
    {
        var (context, item, _, location, schoolClass, userProfile) = await CreateContextAsync();
        await using var _ = context;
        var import = await AddPendingImportAsync(context, schoolClass.Id);
        var handler = new ConfirmGoodsReceiptImportCommandHandler(context, new FakeUser(userProfile.IdentityId));

        var command = new ConfirmGoodsReceiptImportCommand
        {
            ImportId = import.Id,
            Note = "Reviewed",
            Lines =
            [
                new ConfirmGoodsReceiptImportLine
                {
                    ItemId = item.Id, LocationId = location.Id, Quantity = 12, UnitPrice = 2m, SourceLineIndex = 0
                }
            ]
        };

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Lines.Count.ShouldBe(1);
        result.Value.Lines[0].ItemName.ShouldBe("Rice");
        result.Value.Lines[0].Quantity.ShouldBe(12);

        var batch = await context.StockBatches.SingleAsync(CancellationToken.None);
        batch.ItemId.ShouldBe(item.Id);
        batch.LocationId.ShouldBe(location.Id);
        batch.ReceivedClassId.ShouldBe(schoolClass.Id);

        var transaction = await context.StockTransactions.SingleAsync(CancellationToken.None);
        transaction.Type.ShouldBe(StockTransactionType.Order);
        transaction.UserId.ShouldBe(userProfile.IdentityId);
    }

    [Test]
    public async Task Handle_MarksImportConfirmedWithResultingReceiptId()
    {
        var (context, item, _, location, schoolClass, userProfile) = await CreateContextAsync();
        await using var _ = context;
        var import = await AddPendingImportAsync(context, schoolClass.Id);
        var handler = new ConfirmGoodsReceiptImportCommandHandler(context, new FakeUser(userProfile.IdentityId));

        var result = await handler.Handle(new ConfirmGoodsReceiptImportCommand
        {
            ImportId = import.Id,
            Lines = [new ConfirmGoodsReceiptImportLine { ItemId = item.Id, LocationId = location.Id, Quantity = 5, UnitPrice = 1m }]
        }, CancellationToken.None);

        var reloaded = await context.GoodsReceiptImports.SingleAsync(i => i.Id == import.Id, CancellationToken.None);
        reloaded.Status.ShouldBe(GoodsReceiptImportStatus.Confirmed);
        reloaded.ResultingGoodsReceiptId.ShouldBe(result.Value.Id);
    }

    [Test]
    public async Task Handle_WithNewItem_CreatesItemMatchingExistingCategoryByName()
    {
        var (context, _, category, location, schoolClass, userProfile) = await CreateContextAsync();
        await using var _ = context;
        var import = await AddPendingImportAsync(context, schoolClass.Id);
        var handler = new ConfirmGoodsReceiptImportCommandHandler(context, new FakeUser(userProfile.IdentityId));

        var command = new ConfirmGoodsReceiptImportCommand
        {
            ImportId = import.Id,
            Lines =
            [
                new ConfirmGoodsReceiptImportLine
                {
                    Name = "Beans", Sku = "SKU-NEW", Unit = "kg", CategoryName = "pantry",
                    LocationId = location.Id, Quantity = 4, UnitPrice = 3m, SourceLineIndex = 0
                }
            ]
        };

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        (await context.Categories.CountAsync(CancellationToken.None)).ShouldBe(1);

        var newItem = await context.Items.SingleAsync(i => i.Name == "Beans", CancellationToken.None);
        newItem.Sku.ShouldBe("SKU-NEW");
        newItem.CategoryId.ShouldBe(category.Id);
        newItem.IsActive.ShouldBeTrue();
    }

    [Test]
    public async Task Handle_WithNewItem_CreatesMissingCategory()
    {
        var (context, _, _, location, schoolClass, userProfile) = await CreateContextAsync();
        await using var _ = context;
        var import = await AddPendingImportAsync(context, schoolClass.Id);
        var handler = new ConfirmGoodsReceiptImportCommandHandler(context, new FakeUser(userProfile.IdentityId));

        await handler.Handle(new ConfirmGoodsReceiptImportCommand
        {
            ImportId = import.Id,
            Lines =
            [
                new ConfirmGoodsReceiptImportLine
                {
                    Name = "Detergent", Unit = "pcs", CategoryName = "Cleaning",
                    LocationId = location.Id, Quantity = 2, UnitPrice = 5m
                }
            ]
        }, CancellationToken.None);

        (await context.Categories.AnyAsync(c => c.Name == "Cleaning", CancellationToken.None)).ShouldBeTrue();
        (await context.Categories.CountAsync(CancellationToken.None)).ShouldBe(2);
    }

    [Test]
    public async Task Handle_WithSplitLinesForSameSource_CreatesOneBatchPerLine()
    {
        var (context, item, _, location, schoolClass, userProfile) = await CreateContextAsync();
        await using var _ = context;
        var secondLocation = new Location { Name = "Annex", Type = "StorageRoom" };
        context.Locations.Add(secondLocation);
        await context.SaveChangesAsync(CancellationToken.None);

        var import = await AddPendingImportAsync(context, schoolClass.Id);
        var handler = new ConfirmGoodsReceiptImportCommandHandler(context, new FakeUser(userProfile.IdentityId));

        await handler.Handle(new ConfirmGoodsReceiptImportCommand
        {
            ImportId = import.Id,
            Lines =
            [
                new ConfirmGoodsReceiptImportLine { ItemId = item.Id, LocationId = location.Id, Quantity = 6, UnitPrice = 2m, SourceLineIndex = 0 },
                new ConfirmGoodsReceiptImportLine { ItemId = item.Id, LocationId = secondLocation.Id, Quantity = 4, UnitPrice = 2m, SourceLineIndex = 0 }
            ]
        }, CancellationToken.None);

        (await context.StockBatches.CountAsync(CancellationToken.None)).ShouldBe(2);
        (await context.StockTransactions.CountAsync(CancellationToken.None)).ShouldBe(2);
    }

    [Test]
    public async Task Handle_WhenAlreadyConfirmed_ReturnsSameReceiptWithoutCreatingAnother()
    {
        var (context, item, _, location, schoolClass, userProfile) = await CreateContextAsync();
        await using var _ = context;
        var import = await AddPendingImportAsync(context, schoolClass.Id);
        var handler = new ConfirmGoodsReceiptImportCommandHandler(context, new FakeUser(userProfile.IdentityId));

        var command = new ConfirmGoodsReceiptImportCommand
        {
            ImportId = import.Id,
            Lines = [new ConfirmGoodsReceiptImportLine { ItemId = item.Id, LocationId = location.Id, Quantity = 5, UnitPrice = 1m }]
        };

        var first = await handler.Handle(command, CancellationToken.None);
        var second = await handler.Handle(command, CancellationToken.None);

        second.IsSuccess.ShouldBeTrue();
        second.Value.Id.ShouldBe(first.Value.Id);
        (await context.GoodsReceipts.CountAsync(CancellationToken.None)).ShouldBe(1);
    }

    [Test]
    public async Task Handle_WhenImportNotInReview_ReturnsFailure()
    {
        var (context, item, _, location, schoolClass, userProfile) = await CreateContextAsync();
        await using var _ = context;
        var import = GoodsReceiptImport.Create(schoolClass.Id, Guid.NewGuid(), Guid.NewGuid(), "blob/path.pdf");
        import.Status = GoodsReceiptImportStatus.Failed;
        context.GoodsReceiptImports.Add(import);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new ConfirmGoodsReceiptImportCommandHandler(context, new FakeUser(userProfile.IdentityId));

        var result = await handler.Handle(new ConfirmGoodsReceiptImportCommand
        {
            ImportId = import.Id,
            Lines = [new ConfirmGoodsReceiptImportLine { ItemId = item.Id, LocationId = location.Id, Quantity = 5, UnitPrice = 1m }]
        }, CancellationToken.None);

        result.IsFailed.ShouldBeTrue();
        (await context.GoodsReceipts.CountAsync(CancellationToken.None)).ShouldBe(0);
    }

    [Test]
    public async Task Handle_WhenImportDoesNotExist_ReturnsFailure()
    {
        var (context, item, _, location, _, userProfile) = await CreateContextAsync();
        await using var _ = context;
        var handler = new ConfirmGoodsReceiptImportCommandHandler(context, new FakeUser(userProfile.IdentityId));

        var result = await handler.Handle(new ConfirmGoodsReceiptImportCommand
        {
            ImportId = Guid.NewGuid(),
            Lines = [new ConfirmGoodsReceiptImportLine { ItemId = item.Id, LocationId = location.Id, Quantity = 5, UnitPrice = 1m }]
        }, CancellationToken.None);

        result.IsFailed.ShouldBeTrue();
    }
}

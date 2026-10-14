using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;
using skestock.Application.Features.GoodsReceipts.Commands.ConfirmGoodsReceiptImport;
using skestock.Application.Features.GoodsReceipts.Models;
using skestock.Application.UnitTests.Features.GoodsReceipts.Commands.CreateGoodsReceipt;
using skestock.Domain.Entities;
using skestock.Domain.Entities.Categories;
using skestock.Domain.Entities.GoodsReceipts;
using skestock.Domain.Entities.Items;
using skestock.Domain.Entities.Locations;
using skestock.Domain.Entities.SchoolClasses;
using skestock.Domain.Entities.Users;
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
    public async Task Handle_WithExtractedReceivedAt_UsesItForReceiptAndBatches()
    {
        var (context, item, _, location, schoolClass, userProfile) = await CreateContextAsync();
        await using var _ = context;
        var import = await AddPendingImportAsync(context, schoolClass.Id);
        var extractedDate = new DateOnly(2026, 8, 17);
        import.ApplyExtractionResult(new GoodsReceiptExtractionResult { ReceivedAt = extractedDate }.ToJson());
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new ConfirmGoodsReceiptImportCommandHandler(context, new FakeUser(userProfile.IdentityId));

        var result = await handler.Handle(new ConfirmGoodsReceiptImportCommand
        {
            ImportId = import.Id,
            Lines = [new ConfirmGoodsReceiptImportLine { ItemId = item.Id, LocationId = location.Id, Quantity = 5, UnitPrice = 1m }]
        }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();

        var receipt = await context.GoodsReceipts.SingleAsync(CancellationToken.None);
        receipt.ReceivedAt.ShouldBe(new DateTimeOffset(
            extractedDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero));

        var batch = await context.StockBatches.SingleAsync(CancellationToken.None);
        batch.ReceivedDate.ShouldBe(extractedDate);
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
        context.ChangeTracker.Clear();
        var snapshot = await context.GoodsReceiptImportLines.SingleAsync();
        snapshot.MatchedItemId.ShouldBe(newItem.Id);
        snapshot.RawItemText.ShouldBe("Beans");
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
        (await context.GoodsReceiptImportLines.CountAsync(CancellationToken.None)).ShouldBe(0);
    }

    [Test]
    public async Task Handle_ExistingPerishableItemWithShelfLifeAndNoExpiry_DerivesExpiry()
    {
        var (context, _, category, location, schoolClass, userProfile) = await CreateContextAsync();
        await using var _ = context;

        var perishable = new Item { Sku = "SKU-P", Name = "Milk", Unit = "L", IsPerishable = true, ShelfLifeDays = 10, Category = category, CategoryId = category.Id };
        context.Items.Add(perishable);
        await context.SaveChangesAsync(CancellationToken.None);

        var import = await AddPendingImportAsync(context, schoolClass.Id);
        var handler = new ConfirmGoodsReceiptImportCommandHandler(context, new FakeUser(userProfile.IdentityId));

        var result = await handler.Handle(new ConfirmGoodsReceiptImportCommand
        {
            ImportId = import.Id,
            Lines = [new ConfirmGoodsReceiptImportLine { ItemId = perishable.Id, LocationId = location.Id, Quantity = 3, UnitPrice = 2m }]
        }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();

        var batch = await context.StockBatches.SingleAsync(b => b.ItemId == perishable.Id, CancellationToken.None);
        batch.ExpiryDate.ShouldBe(batch.ReceivedDate.AddDays(10));
        context.ChangeTracker.Clear();
        (await context.GoodsReceiptImportLines.SingleAsync()).ExpiryDate.ShouldBe(batch.ExpiryDate);
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
    [TestCase(null, 0, "Rice")]
    [TestCase("  ", 0, "Rice")]
    [TestCase("  Original rice text  ", 0, "  Original rice text  ")]
    [TestCase("Original rice text", -1, "Rice")]
    [TestCase("Original rice text", 1, "Rice")]
    public async Task Handle_PersistsReviewedSnapshotWithSourceTextFallback(string? sourceName, int sourceIndex, string expectedText)
    {
        var (context, item, _, location, schoolClass, userProfile) = await CreateContextAsync();
        await using var _ = context;
        var import = await AddPendingImportAsync(context, schoolClass.Id);
        import.ApplyExtractionResult(new GoodsReceiptExtractionResult
        {
            LineItems = [new GoodsReceiptLineItem { Name = sourceName }]
        }.ToJson());
        await context.SaveChangesAsync(CancellationToken.None);
        var handler = new ConfirmGoodsReceiptImportCommandHandler(context, new FakeUser(userProfile.IdentityId));
        var expiry = new DateOnly(2027, 1, 5);
        item.IsPerishable = true;
        await context.SaveChangesAsync(CancellationToken.None);

        var result = await handler.Handle(new ConfirmGoodsReceiptImportCommand
        {
            ImportId = import.Id,
            Lines = [new ConfirmGoodsReceiptImportLine
            {
                ItemId = item.Id, LocationId = location.Id, Quantity = 12,
                UnitPrice = 2.75m, ExpiryDate = expiry, SourceLineIndex = sourceIndex
            }]
        }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        context.ChangeTracker.Clear();
        var snapshot = await context.GoodsReceiptImportLines.SingleAsync();
        snapshot.ImportId.ShouldBe(import.Id);
        snapshot.MatchedItemId.ShouldBe(item.Id);
        snapshot.LocationId.ShouldBe(location.Id);
        snapshot.RawItemText.ShouldBe(expectedText);
        snapshot.Quantity.ShouldBe(12);
        snapshot.UnitPrice.ShouldBe(2.75m);
        snapshot.ExpiryDate.ShouldBe(expiry);
    }

    [Test]
    public async Task Handle_SplitNewItem_PersistsSeparateSnapshotsWithSharedResolvedItem()
    {
        var (context, _, _, location, schoolClass, userProfile) = await CreateContextAsync();
        await using var _ = context;
        var annex = new Location { Name = "Annex", Type = "StorageRoom" };
        context.Locations.Add(annex);
        var import = await AddPendingImportAsync(context, schoolClass.Id);
        import.ApplyExtractionResult(new GoodsReceiptExtractionResult
        {
            LineItems = [new GoodsReceiptLineItem { Name = "Extracted beans", Quantity = 10 }]
        }.ToJson());
        await context.SaveChangesAsync(CancellationToken.None);
        var handler = new ConfirmGoodsReceiptImportCommandHandler(context, new FakeUser(userProfile.IdentityId));

        var result = await handler.Handle(new ConfirmGoodsReceiptImportCommand
        {
            ImportId = import.Id,
            Lines =
            [
                new ConfirmGoodsReceiptImportLine { Name = "Beans", CategoryName = "Pantry", LocationId = location.Id, Quantity = 6, UnitPrice = 3m },
                new ConfirmGoodsReceiptImportLine { Name = "Beans", CategoryName = "Pantry", LocationId = annex.Id, Quantity = 4, UnitPrice = 3m }
            ]
        }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        context.ChangeTracker.Clear();
        var beans = await context.Items.SingleAsync(i => i.Name == "Beans");
        var snapshots = await context.GoodsReceiptImportLines.OrderByDescending(l => l.Quantity).ToListAsync();
        snapshots.Count.ShouldBe(2);
        snapshots.Select(l => l.MatchedItemId).ShouldAllBe(id => id == beans.Id);
        snapshots.Select(l => l.RawItemText).ShouldAllBe(text => text == "Extracted beans");
        snapshots[0].LocationId.ShouldBe(location.Id);
        snapshots[0].Quantity.ShouldBe(6);
        snapshots[1].LocationId.ShouldBe(annex.Id);
        snapshots[1].Quantity.ShouldBe(4);
    }

    [Test]
    public async Task Handle_StockEditsAndRepeatConfirmation_PreserveSnapshot()
    {
        var (context, item, _, location, schoolClass, userProfile) = await CreateContextAsync();
        await using var _ = context;
        var import = await AddPendingImportAsync(context, schoolClass.Id);
        var handler = new ConfirmGoodsReceiptImportCommandHandler(context, new FakeUser(userProfile.IdentityId));
        var command = new ConfirmGoodsReceiptImportCommand
        {
            ImportId = import.Id,
            Lines = [new ConfirmGoodsReceiptImportLine { ItemId = item.Id, LocationId = location.Id, Quantity = 5, UnitPrice = 2m }]
        };
        var first = await handler.Handle(command, CancellationToken.None);
        context.ChangeTracker.Clear();
        var original = await context.GoodsReceiptImportLines.AsNoTracking().SingleAsync();
        var batch = await context.StockBatches.SingleAsync();
        batch.Quantity = 1;
        batch.UnitPrice = 9m;
        batch.ExpiryDate = new DateOnly(2027, 2, 1);
        await context.SaveChangesAsync(CancellationToken.None);
        context.ChangeTracker.Clear();

        var second = await handler.Handle(command, CancellationToken.None);

        second.Value.Id.ShouldBe(first.Value.Id);
        context.ChangeTracker.Clear();
        var snapshot = await context.GoodsReceiptImportLines.SingleAsync();
        snapshot.Id.ShouldBe(original.Id);
        snapshot.Quantity.ShouldBe(5);
        snapshot.UnitPrice.ShouldBe(2m);
        snapshot.ExpiryDate.ShouldBeNull();
        snapshot.RawItemText.ShouldBe("Rice");
        snapshot.MatchedItemId.ShouldBe(item.Id);
        snapshot.LocationId.ShouldBe(location.Id);
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase(null)]
    public async Task Handle_UnusableSnapshotText_DoesNotPersistConfirmation(string? itemName)
    {
        var (context, item, _, location, schoolClass, userProfile) = await CreateContextAsync();
        await using var _ = context;
        item.Name = itemName!;
        await context.SaveChangesAsync(CancellationToken.None);
        var import = await AddPendingImportAsync(context, schoolClass.Id);
        var handler = new ConfirmGoodsReceiptImportCommandHandler(context, new FakeUser(userProfile.IdentityId));

        Should.Throw<skestock.Application.Common.Exceptions.ValidationException>(async () =>
            await handler.Handle(new ConfirmGoodsReceiptImportCommand
            {
                ImportId = import.Id,
                Lines = [new ConfirmGoodsReceiptImportLine { ItemId = item.Id, LocationId = location.Id, Quantity = 5 }]
            }, CancellationToken.None));

        context.ChangeTracker.Clear();
        (await context.GoodsReceiptImportLines.CountAsync()).ShouldBe(0);
        (await context.GoodsReceipts.CountAsync()).ShouldBe(0);
        (await context.GoodsReceiptImports.SingleAsync()).Status.ShouldBe(GoodsReceiptImportStatus.PendingReview);
    }

    [TestCase(500, false)]
    [TestCase(501, false)]
    [TestCase(500, true)]
    [TestCase(501, true)]
    public async Task Handle_SnapshotTextLength_RespectsPersistenceLimit(int length, bool newItem)
    {
        var (context, item, _, location, schoolClass, userProfile) = await CreateContextAsync();
        await using var _ = context;
        var import = await AddPendingImportAsync(context, schoolClass.Id);
        var text = new string('x', length);
        if (!newItem)
        {
            import.ApplyExtractionResult(new GoodsReceiptExtractionResult
            {
                LineItems = [new GoodsReceiptLineItem { Name = text }]
            }.ToJson());
            await context.SaveChangesAsync(CancellationToken.None);
        }
        var handler = new ConfirmGoodsReceiptImportCommandHandler(context, new FakeUser(userProfile.IdentityId));
        var command = new ConfirmGoodsReceiptImportCommand
        {
            ImportId = import.Id,
            Lines = [new ConfirmGoodsReceiptImportLine
            {
                ItemId = newItem ? null : item.Id, Name = newItem ? text : null,
                CategoryName = "New category", LocationId = location.Id, Quantity = 5
            }]
        };

        if (length <= 500)
        {
            (await handler.Handle(command, CancellationToken.None)).IsSuccess.ShouldBeTrue();
            context.ChangeTracker.Clear();
            (await context.GoodsReceiptImportLines.SingleAsync()).RawItemText.ShouldBe(text);
        }
        else
        {
            var exception = Should.Throw<skestock.Application.Common.Exceptions.ValidationException>(async () =>
                await handler.Handle(command, CancellationToken.None));
            exception.Errors["Lines"].Single().Code.ShouldBe("validation.max_length");
            context.ChangeTracker.Clear();
            (await context.GoodsReceiptImportLines.CountAsync()).ShouldBe(0);
            (await context.GoodsReceipts.CountAsync()).ShouldBe(0);
            (await context.StockBatches.CountAsync()).ShouldBe(0);
            (await context.Items.CountAsync()).ShouldBe(1);
            (await context.Categories.CountAsync()).ShouldBe(1);
            (await context.GoodsReceiptImports.SingleAsync()).Status.ShouldBe(GoodsReceiptImportStatus.PendingReview);
        }
    }

}

using skestock.Application.Common.Exceptions;
using skestock.Application.Features.GoodsReceipts.Commands.ConfirmGoodsReceiptImport;
using skestock.Domain.Entities;
using skestock.Domain.Entities.Categories;
using skestock.Domain.Entities.GoodsReceipts;
using skestock.Domain.Entities.Items;
using skestock.Domain.Entities.Locations;
using skestock.Domain.Entities.SchoolClasses;
using skestock.Domain.Entities.Stock;
using skestock.Domain.Entities.Storage;
using skestock.Domain.Entities.Users;
using skestock.Domain.Enums;

namespace skestock.Application.FunctionalTests.Features.GoodsReceipts.Commands.ConfirmGoodsReceiptImport;

public class ConfirmGoodsReceiptImportCommandTests : TestBase
{
    private string _prefix = null!;

    [SetUp]
    public void SetUpPrefix()
    {
        _prefix = $"FT{Guid.NewGuid():N}"[..10];
    }

    private async Task<(SchoolClass Class, Location Location, GoodsReceiptImport Import)> SeedPendingImportAsync(
        GoodsReceiptImportStatus status = GoodsReceiptImportStatus.PendingReview)
    {
        var schoolClass = new SchoolClass
        {
            Name = $"{_prefix}-Class",
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 6, 1)
        };
        await TestApp.AddAsync(schoolClass);

        var location = new Location { Name = $"{_prefix}-Loc", Type = "Kitchen" };
        await TestApp.AddAsync(location);

        var file = new FileMetadata
        {
            FileId = Guid.NewGuid(),
            OriginalName = $"{_prefix}-receipt.pdf",
            BlobContainer = "app-files",
            BlobPath = $"imports/{_prefix}/receipt.pdf",
            ContentType = "application/pdf",
            SizeBytes = 2048,
            Status = FileStatus.Completed
        };
        await TestApp.AddAsync(file);

        // GoodsReceiptImport.UploadedByUserId and StockTransaction.UserId are FKs to UserProfile.IdentityId.
        var userId = await TestApp.RunAsDefaultUserAsync();
        await TestApp.AddAsync(new UserProfile { IdentityId = userId!.Value, FirstName = "Staff", LastName = "Member" });

        var import = GoodsReceiptImport.Create(schoolClass.Id, file.Id, userId.Value, file.BlobPath);
        import.Status = status;
        await TestApp.AddAsync(import);

        return (schoolClass, location, import);
    }

    private async Task<Category> SeedCategoryAsync(string suffix = "Category", Guid? parentId = null)
    {
        var category = new Category { Name = $"{_prefix}-{suffix}", ParentId = parentId };
        await TestApp.AddAsync(category);
        return category;
    }

    private static ConfirmGoodsReceiptImportCommand Command(Guid importId, params ConfirmGoodsReceiptImportLine[] lines) =>
        new() { ImportId = importId, Note = "Reviewed", Lines = [.. lines] };

    [Test]
    public async Task Handle_WithExistingItem_CreatesReceiptBatchAndConfirmsImport()
    {
        var (schoolClass, location, import) = await SeedPendingImportAsync();
        var category = await SeedCategoryAsync();
        var item = new Item { Name = $"{_prefix}-Rice", Unit = "kg", CategoryId = category.Id };
        await TestApp.AddAsync(item);

        var result = await TestApp.SendAsync(Command(import.Id, new ConfirmGoodsReceiptImportLine
        {
            ItemId = item.Id, LocationId = location.Id, Quantity = 12, UnitPrice = 2m
        }));

        result.IsSuccess.ShouldBeTrue();
        result.Value.Lines.Count.ShouldBe(1);

        var batch = await TestApp.SingleOrDefaultAsync<StockBatch>(b => b.ItemId == item.Id);
        batch.ShouldNotBeNull();
        batch.Quantity.ShouldBe(12);
        batch.LocationId.ShouldBe(location.Id);
        batch.ReceivedClassId.ShouldBe(schoolClass.Id);

        var persisted = await TestApp.FindAsync<GoodsReceiptImport>(import.Id);
        persisted!.Status.ShouldBe(GoodsReceiptImportStatus.Confirmed);
        persisted.ResultingGoodsReceiptId.ShouldNotBeNull();
    }

    [Test]
    public async Task Handle_WithNewItemAndNewCategoryName_CreatesBoth()
    {
        var (_, location, import) = await SeedPendingImportAsync();
        var categoryName = $"{_prefix}-Fresh";

        var result = await TestApp.SendAsync(Command(import.Id, new ConfirmGoodsReceiptImportLine
        {
            Name = $"{_prefix}-Beans", Unit = "kg", CategoryName = categoryName,
            LocationId = location.Id, Quantity = 4, UnitPrice = 3m
        }));

        result.IsSuccess.ShouldBeTrue();
        var category = await TestApp.SingleOrDefaultAsync<Category>(c => c.Name == categoryName);
        category.ShouldNotBeNull();
        category.ParentId.ShouldBeNull();
        var item = await TestApp.SingleOrDefaultAsync<Item>(i => i.Name == $"{_prefix}-Beans");
        item.ShouldNotBeNull();
        item.CategoryId.ShouldBe(category.Id);
    }

    [Test]
    public async Task Handle_WithNewItemInLeafCategory_UsesExistingCategory()
    {
        var (_, location, import) = await SeedPendingImportAsync();
        var parent = await SeedCategoryAsync("Parent");
        var leaf = await SeedCategoryAsync("Leaf", parent.Id);

        var result = await TestApp.SendAsync(Command(import.Id, new ConfirmGoodsReceiptImportLine
        {
            Name = $"{_prefix}-Milk", Unit = "l", CategoryName = leaf.Name,
            LocationId = location.Id, Quantity = 2, UnitPrice = 5m
        }));

        result.IsSuccess.ShouldBeTrue();
        (await TestApp.SingleOrDefaultAsync<Item>(i => i.Name == $"{_prefix}-Milk"))!.CategoryId.ShouldBe(leaf.Id);
    }

    [Test]
    public async Task Handle_WithNewItemInParentCategory_ThrowsAndLeavesImportPending()
    {
        var (_, location, import) = await SeedPendingImportAsync();
        var parent = await SeedCategoryAsync("Parent");
        await SeedCategoryAsync("Leaf", parent.Id);

        var act = async () => await TestApp.SendAsync(Command(import.Id, new ConfirmGoodsReceiptImportLine
        {
            Name = $"{_prefix}-Cheese", Unit = "kg", CategoryName = parent.Name,
            LocationId = location.Id, Quantity = 2, UnitPrice = 5m
        }));

        var exception = await act.ShouldThrowAsync<ValidationException>();
        exception.Errors.ShouldContainKey(nameof(ConfirmGoodsReceiptImportCommand.Lines));
        (await TestApp.SingleOrDefaultAsync<Item>(i => i.Name == $"{_prefix}-Cheese")).ShouldBeNull();
        (await TestApp.FindAsync<GoodsReceiptImport>(import.Id))!.Status.ShouldBe(GoodsReceiptImportStatus.PendingReview);
    }

    [Test]
    public async Task Handle_WhenImportIsStillProcessing_ReturnsFailure()
    {
        var (_, location, import) = await SeedPendingImportAsync(GoodsReceiptImportStatus.Processing);
        var category = await SeedCategoryAsync();
        var item = new Item { Name = $"{_prefix}-Rice", Unit = "kg", CategoryId = category.Id };
        await TestApp.AddAsync(item);

        var result = await TestApp.SendAsync(Command(import.Id, new ConfirmGoodsReceiptImportLine
        {
            ItemId = item.Id, LocationId = location.Id, Quantity = 1, UnitPrice = 1m
        }));

        result.IsFailed.ShouldBeTrue();
        (await TestApp.SingleOrDefaultAsync<StockBatch>(b => b.ItemId == item.Id)).ShouldBeNull();
    }

    [Test]
    public async Task Handle_WhenConfirmedTwice_ReturnsTheSameReceiptWithoutDuplicatingStock()
    {
        var (_, location, import) = await SeedPendingImportAsync();
        var category = await SeedCategoryAsync();
        var item = new Item { Name = $"{_prefix}-Rice", Unit = "kg", CategoryId = category.Id };
        await TestApp.AddAsync(item);
        var command = Command(import.Id, new ConfirmGoodsReceiptImportLine
        {
            ItemId = item.Id, LocationId = location.Id, Quantity = 3, UnitPrice = 2m
        });

        var first = await TestApp.SendAsync(command);
        var second = await TestApp.SendAsync(command);

        second.IsSuccess.ShouldBeTrue();
        second.Value.Id.ShouldBe(first.Value.Id);
        (await TestApp.SingleOrDefaultAsync<StockBatch>(b => b.ItemId == item.Id))!.Quantity.ShouldBe(3);
    }
}

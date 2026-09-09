using skestock.Application.Features.Items.Commands.ConfirmItemImport;
using skestock.Domain.Entities;
using skestock.Domain.Enums;

namespace skestock.Application.FunctionalTests.Features.Items.Commands.ConfirmItemImport;

public class ConfirmItemImportCommandTests : TestBase
{
    private string _prefix = null!;

    [SetUp]
    public void SetUpPrefix()
    {
        _prefix = $"FT{Guid.NewGuid():N}"[..10];
    }

    private async Task<ItemImport> SeedPendingReviewImportAsync()
    {
        var userId = await TestApp.RunAsDefaultUserAsync();
        await TestApp.AddAsync(new UserProfile { IdentityId = userId!.Value, FirstName = "Staff", LastName = "Member" });

        var file = new FileMetadata
        {
            FileId = Guid.NewGuid(),
            OriginalName = $"{_prefix}-items.pdf",
            BlobContainer = "app-files",
            BlobPath = $"imports/{_prefix}/items.pdf",
            ContentType = "application/pdf",
            SizeBytes = 2048,
            Status = FileStatus.Completed
        };
        await TestApp.AddAsync(file);

        var import = ItemImport.Create(file.Id, userId.Value, file.BlobPath);
        import.Status = ItemImportStatus.PendingReview;
        await TestApp.AddAsync(import);
        return import;
    }

    [Test]
    public async Task Handle_WithReviewedItems_CreatesItemsAndCategoriesAndMarksConfirmed()
    {
        var import = await SeedPendingReviewImportAsync();
        var categoryName = $"{_prefix}-Dairy";
        var sku = $"{_prefix}-SKU-1";

        var result = await TestApp.SendAsync(new ConfirmItemImportCommand
        {
            ImportId = import.Id,
            Items =
            [
                new ConfirmItemImportItem { Sku = sku, Name = "Milk", CategoryName = categoryName, Unit = "L" }
            ]
        });

        result.IsSuccess.ShouldBeTrue();
        result.Value.Status.ShouldBe(ItemImportStatus.Confirmed);
        result.Value.Items.Count.ShouldBe(1);
        result.Value.Items[0].Created.ShouldBeTrue();
        result.Value.Items[0].CategoryCreated.ShouldBeTrue();

        var category = await TestApp.SingleOrDefaultAsync<Category>(c => c.Name == categoryName);
        category.ShouldNotBeNull();

        var item = await TestApp.SingleOrDefaultAsync<Item>(i => i.Sku == sku);
        item.ShouldNotBeNull();
        item.CategoryId.ShouldBe(category.Id);

        var persisted = await TestApp.FindAsync<ItemImport>(import.Id);
        persisted.ShouldNotBeNull();
        persisted.Status.ShouldBe(ItemImportStatus.Confirmed);
    }

    [Test]
    public async Task Handle_WhenSkuAlreadyExists_ReusesExistingItemInsteadOfCreatingDuplicate()
    {
        var import = await SeedPendingReviewImportAsync();
        var categoryName = $"{_prefix}-Dairy";
        var sku = $"{_prefix}-SKU-1";

        var category = new Category { Name = categoryName };
        await TestApp.AddAsync(category);
        var existingItem = new Item { Sku = sku, Name = "Milk 1L", Unit = "L", CategoryId = category.Id };
        await TestApp.AddAsync(existingItem);

        var result = await TestApp.SendAsync(new ConfirmItemImportCommand
        {
            ImportId = import.Id,
            Items = [new ConfirmItemImportItem { Sku = sku, Name = "Milk", CategoryName = categoryName, Unit = "L" }]
        });

        result.IsSuccess.ShouldBeTrue();
        result.Value.Items.Count.ShouldBe(1);
        result.Value.Items[0].Created.ShouldBeFalse();
        result.Value.Items[0].Id.ShouldBe(existingItem.Id);
    }
}

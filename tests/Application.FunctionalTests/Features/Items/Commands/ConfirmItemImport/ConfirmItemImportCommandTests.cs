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
    public async Task Handle_WithReviewedItems_UsesSelectedItemAndMarksConfirmed()
    {
        var import = await SeedPendingReviewImportAsync();
        var categoryName = $"{_prefix}-Dairy";
        var sku = $"{_prefix}-SKU-1";
        var category = new Category { Name = categoryName };
        await TestApp.AddAsync(category);
        var item = new Item { Sku = sku, Name = "Milk", Unit = "L", CategoryId = category.Id };
        await TestApp.AddAsync(item);

        var result = await TestApp.SendAsync(new ConfirmItemImportCommand
        {
            ImportId = import.Id,
            Items =
            [
                new ConfirmItemImportItem { ItemId = item.Id, Sku = sku, Name = "Milk", Unit = "L" }
            ]
        });

        result.IsSuccess.ShouldBeTrue();
        result.Value.Status.ShouldBe(ItemImportStatus.Confirmed);
        result.Value.Items.Count.ShouldBe(1);
        result.Value.Items[0].CategoryName.ShouldBe(categoryName);
        result.Value.Items[0].Created.ShouldBeFalse();
        result.Value.Items[0].CategoryCreated.ShouldBeFalse();

        var persistedCategory = await TestApp.SingleOrDefaultAsync<Category>(c => c.Name == categoryName);
        persistedCategory.ShouldNotBeNull();

        var persistedItem = await TestApp.SingleOrDefaultAsync<Item>(i => i.Id == item.Id);
        persistedItem.ShouldNotBeNull();
        persistedItem.CategoryId.ShouldBe(persistedCategory.Id);

        var persisted = await TestApp.FindAsync<ItemImport>(import.Id);
        persisted.ShouldNotBeNull();
        persisted.Status.ShouldBe(ItemImportStatus.Confirmed);
    }

    [Test]
    public async Task Handle_WhenSelectedItemExists_UsesTheSelectedItem()
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
            Items = [new ConfirmItemImportItem { ItemId = existingItem.Id, Sku = sku, Name = "Milk", Unit = "L" }]
        });

        result.IsSuccess.ShouldBeTrue();
        result.Value.Items.Count.ShouldBe(1);
        result.Value.Items[0].Created.ShouldBeFalse();
        result.Value.Items[0].Id.ShouldBe(existingItem.Id);
    }
}

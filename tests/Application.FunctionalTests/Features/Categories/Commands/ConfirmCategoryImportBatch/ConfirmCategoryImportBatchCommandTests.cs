using skestock.Application.Features.Categories.Commands.ConfirmCategoryImportBatch;
using skestock.Domain.Entities;
using skestock.Domain.Enums;

namespace skestock.Application.FunctionalTests.Features.Categories.Commands.ConfirmCategoryImportBatch;

public class ConfirmCategoryImportBatchCommandTests : TestBase
{
    private string _prefix = null!;

    [SetUp]
    public void SetUpPrefix()
    {
        _prefix = $"FT{Guid.NewGuid():N}"[..10];
    }

    private async Task<CategoryImportBatch> SeedPendingReviewImportAsync()
    {
        var userId = await TestApp.RunAsDefaultUserAsync();
        await TestApp.AddAsync(new UserProfile { IdentityId = userId!.Value, FirstName = "Staff", LastName = "Member" });

        var file = new FileMetadata
        {
            FileId = Guid.NewGuid(),
            OriginalName = $"{_prefix}-categories.pdf",
            BlobContainer = "app-files",
            BlobPath = $"imports/{_prefix}/categories.pdf",
            ContentType = "application/pdf",
            SizeBytes = 2048,
            Status = FileStatus.Completed
        };
        await TestApp.AddAsync(file);

        var import = CategoryImportBatch.Create(userId.Value, null, [file.Id]);
        import.Status = CategoryImportBatchStatus.PendingReview;
        await TestApp.AddAsync(import);
        return import;
    }

    [Test]
    public async Task Handle_WithReviewedNames_CreatesCategoriesAndMarksConfirmed()
    {
        var import = await SeedPendingReviewImportAsync();
        var dairyName = $"{_prefix}-Dairy";
        var bakeryName = $"{_prefix}-Bakery";

        var result = await TestApp.SendAsync(new ConfirmCategoryImportBatchCommand
        {
            BatchId = import.Id,
            CategoryNames = [dairyName, bakeryName]
        });

        result.IsSuccess.ShouldBeTrue();
        result.Value.BatchId.ShouldBe(import.Id);
        result.Value.Status.ShouldBe(CategoryImportBatchStatus.Confirmed);

        var dairy = await TestApp.SingleOrDefaultAsync<Category>(c => c.Name == dairyName);
        dairy.ShouldNotBeNull();

        var persisted = await TestApp.FindAsync<CategoryImportBatch>(import.Id);
        persisted.ShouldNotBeNull();
        persisted.Status.ShouldBe(CategoryImportBatchStatus.Confirmed);
    }
}

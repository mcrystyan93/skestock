using skestock.Application.Common.Exceptions;
using skestock.Application.Features.Categories.Commands.CreateCategoryImport;
using skestock.Domain.Entities;
using skestock.Domain.Enums;

namespace skestock.Application.FunctionalTests.Features.Categories.Commands.CreateCategoryImport;

public class CreateCategoryImportCommandTests : TestBase
{
    private string _prefix = null!;

    [SetUp]
    public void SetUpPrefix()
    {
        _prefix = $"FT{Guid.NewGuid():N}"[..10];
    }

    private async Task<FileMetadata> SeedFileMetadataAsync()
    {
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
        return file;
    }

    /// <summary>
    /// CreateCategoryImportCommand is [Authorize]-guarded and writes CategoryImport.UploadedByUserId,
    /// which is a FK to UserProfile.IdentityId (not UserProfile.Id - see CategoryImportConfiguration),
    /// so a matching UserProfile row must be seeded alongside the Identity user TestApp creates.
    /// </summary>
    private async Task RunAsUserWithProfileAsync()
    {
        var userId = await TestApp.RunAsDefaultUserAsync();
        await TestApp.AddAsync(new UserProfile { IdentityId = userId!.Value, FirstName = "Staff", LastName = "Member" });
    }

    [Test]
    public async Task Handle_WithValidData_PersistsProcessingImportAndReturnsDto()
    {
        var file = await SeedFileMetadataAsync();
        await RunAsUserWithProfileAsync();

        var result = await TestApp.SendAsync(new CreateCategoryImportCommand { FileMetadataId = file.Id });

        result.IsSuccess.ShouldBeTrue();
        result.Value.Id.ShouldNotBe(Guid.Empty);
        result.Value.Status.ShouldBe(CategoryImportStatus.Processing);
        result.Value.FileMetadataId.ShouldBe(file.Id);
        result.Value.BlobPath.ShouldBe(file.BlobPath);

        var persisted = await TestApp.FindAsync<CategoryImport>(result.Value.Id);
        persisted.ShouldNotBeNull();
        persisted.Status.ShouldBe(CategoryImportStatus.Processing);
        persisted.FileMetadataId.ShouldBe(file.Id);
        persisted.BlobPath.ShouldBe(file.BlobPath);
        persisted.UploadedByUserId.ShouldBe(TestApp.GetUserId()!.Value);
    }

    [Test]
    public async Task Handle_WithNonExistentFileMetadataId_ThrowsValidationException()
    {
        await RunAsUserWithProfileAsync();

        var act = async () => await TestApp.SendAsync(new CreateCategoryImportCommand { FileMetadataId = Guid.NewGuid() });

        var exception = await act.ShouldThrowAsync<ValidationException>();
        exception.Errors.ShouldContainKey(nameof(CreateCategoryImportCommand.FileMetadataId));
    }
}

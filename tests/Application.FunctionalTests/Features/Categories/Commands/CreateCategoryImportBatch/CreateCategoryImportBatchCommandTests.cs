using skestock.Application.Common.Exceptions;
using skestock.Application.Features.Categories.Commands.CreateCategoryImportBatch;
using skestock.Domain.Entities;
using skestock.Domain.Enums;

namespace skestock.Application.FunctionalTests.Features.Categories.Commands.CreateCategoryImportBatch;

public class CreateCategoryImportBatchCommandTests : TestBase
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
    /// CreateCategoryImportBatchCommand is [Authorize]-guarded and writes CategoryImportBatch.UploadedByUserId,
    /// which is a FK to UserProfile.IdentityId (not UserProfile.Id - see CategoryImportBatchConfiguration),
    /// so a matching UserProfile row must be seeded alongside the Identity user TestApp creates.
    /// </summary>
    private async Task RunAsUserWithProfileAsync()
    {
        var userId = await TestApp.RunAsDefaultUserAsync();
        await TestApp.AddAsync(new UserProfile { IdentityId = userId!.Value, FirstName = "Staff", LastName = "Member" });
    }

    [Test]
    public async Task Handle_WithValidData_PersistsProcessingImportAndReturnsMutationDto()
    {
        var file = await SeedFileMetadataAsync();
        await RunAsUserWithProfileAsync();

        var result = await TestApp.SendAsync(new CreateCategoryImportBatchCommand { FileMetadataIds = [file.Id] });

        result.IsSuccess.ShouldBeTrue();
        result.Value.Id.ShouldNotBe(Guid.Empty);
        result.Value.Status.ShouldBe(CategoryImportBatchStatus.Processing);

        var persisted = await TestApp.FindAsync<CategoryImportBatch>(result.Value.Id);
        persisted.ShouldNotBeNull();
        persisted.Status.ShouldBe(CategoryImportBatchStatus.Processing);
        persisted.Files.Single().FileMetadataId.ShouldBe(file.Id);
        persisted.UploadedByUserId.ShouldBe(TestApp.GetUserId()!.Value);
    }

    [Test]
    public async Task Handle_WithNonExistentFileMetadataId_ThrowsValidationException()
    {
        await RunAsUserWithProfileAsync();

        var act = async () => await TestApp.SendAsync(
            new CreateCategoryImportBatchCommand { FileMetadataIds = [Guid.NewGuid()] });

        var exception = await act.ShouldThrowAsync<ValidationException>();
        exception.Errors.ShouldContainKey(nameof(CreateCategoryImportBatchCommand.FileMetadataIds));
    }
}

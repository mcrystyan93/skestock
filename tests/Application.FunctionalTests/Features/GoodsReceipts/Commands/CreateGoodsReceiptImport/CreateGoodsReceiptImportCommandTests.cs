using skestock.Application.Common.Exceptions;
using skestock.Application.Features.GoodsReceipts.Commands.CreateGoodsReceiptImport;
using skestock.Domain.Entities;
using skestock.Domain.Enums;

namespace skestock.Application.FunctionalTests.Features.GoodsReceipts.Commands.CreateGoodsReceiptImport;

public class CreateGoodsReceiptImportCommandTests : TestBase
{
    private string _prefix = null!;

    [SetUp]
    public void SetUpPrefix()
    {
        _prefix = $"FT{Guid.NewGuid():N}"[..10];
    }

    private async Task<SchoolClass> SeedSchoolClassAsync()
    {
        var schoolClass = new SchoolClass
        {
            Name = $"{_prefix}-Class",
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 6, 1)
        };
        await TestApp.AddAsync(schoolClass);
        return schoolClass;
    }

    private async Task<FileMetadata> SeedFileMetadataAsync()
    {
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
        return file;
    }

    /// <summary>
    /// CreateGoodsReceiptImportCommand is [Authorize]-guarded and writes
    /// GoodsReceiptImport.UploadedByUserId, which is a FK to UserProfile.IdentityId (not
    /// UserProfile.Id - see GoodsReceiptImportConfiguration), so a matching UserProfile row must be
    /// seeded manually alongside the Identity user TestApp creates.
    /// </summary>
    private async Task RunAsUserWithProfileAsync()
    {
        var userId = await TestApp.RunAsDefaultUserAsync();
        await TestApp.AddAsync(new UserProfile { IdentityId = userId!.Value, FirstName = "Staff", LastName = "Member" });
    }

    [Test]
    public async Task Handle_WithValidData_PersistsProcessingImportAndReturnsDto()
    {
        var schoolClass = await SeedSchoolClassAsync();
        var file = await SeedFileMetadataAsync();
        await RunAsUserWithProfileAsync();

        var result = await TestApp.SendAsync(new CreateGoodsReceiptImportCommand
        {
            ClassId = schoolClass.Id,
            FileMetadataId = file.Id
        });

        result.IsSuccess.ShouldBeTrue();
        result.Value.Id.ShouldNotBe(Guid.Empty);
        result.Value.Status.ShouldBe(GoodsReceiptImportStatus.Processing);
        result.Value.ClassId.ShouldBe(schoolClass.Id);
        result.Value.FileMetadataId.ShouldBe(file.Id);
        result.Value.BlobPath.ShouldBe(file.BlobPath);

        var persisted = await TestApp.FindAsync<GoodsReceiptImport>(result.Value.Id);
        persisted.ShouldNotBeNull();
        persisted.Status.ShouldBe(GoodsReceiptImportStatus.Processing);
        persisted.ClassId.ShouldBe(schoolClass.Id);
        persisted.FileMetadataId.ShouldBe(file.Id);
        persisted.BlobPath.ShouldBe(file.BlobPath);
        persisted.UploadedByUserId.ShouldBe(TestApp.GetUserId()!.Value);
    }

    [Test]
    public async Task Handle_WithNonExistentClassId_ThrowsValidationException()
    {
        var file = await SeedFileMetadataAsync();
        await RunAsUserWithProfileAsync();

        var act = async () => await TestApp.SendAsync(new CreateGoodsReceiptImportCommand
        {
            ClassId = Guid.NewGuid(),
            FileMetadataId = file.Id
        });

        var exception = await act.ShouldThrowAsync<ValidationException>();
        exception.Errors.ShouldContainKey(nameof(CreateGoodsReceiptImportCommand.ClassId));
    }

    [Test]
    public async Task Handle_WithNonExistentFileMetadataId_ThrowsValidationException()
    {
        var schoolClass = await SeedSchoolClassAsync();
        await RunAsUserWithProfileAsync();

        var act = async () => await TestApp.SendAsync(new CreateGoodsReceiptImportCommand
        {
            ClassId = schoolClass.Id,
            FileMetadataId = Guid.NewGuid()
        });

        var exception = await act.ShouldThrowAsync<ValidationException>();
        exception.Errors.ShouldContainKey(nameof(CreateGoodsReceiptImportCommand.FileMetadataId));
    }
}

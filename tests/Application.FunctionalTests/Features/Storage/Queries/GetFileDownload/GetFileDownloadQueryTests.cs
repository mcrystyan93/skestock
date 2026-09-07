using skestock.Application.Common.Errors;
using skestock.Application.Storage.Queries.GetFileDownload;
using skestock.Domain.Entities;
using skestock.Domain.Enums;

namespace skestock.Application.FunctionalTests.Features.Storage.Queries.GetFileDownload;

public class GetFileDownloadQueryTests : TestBase
{
    private static FileMetadata NewFile(Guid fileId, FileStatus status) => new()
    {
        FileId = fileId,
        OriginalName = "receipt.pdf",
        BlobContainer = "app-files",
        BlobPath = $"{fileId}/receipt.pdf",
        ContentType = "application/pdf",
        Status = status
    };

    [Test]
    public async Task ReturnsMetadataAndDownloadUri_WhenFileCompleted()
    {
        var fileId = Guid.NewGuid();
        var file = NewFile(fileId, FileStatus.Completed);
        await TestApp.AddAsync(file);

        var result = await TestApp.SendAsync(new GetFileDownloadQuery { FileId = fileId });

        result.IsSuccess.ShouldBeTrue();
        result.Value.File.FileId.ShouldBe(fileId);
        result.Value.File.OriginalName.ShouldBe(file.OriginalName);
        result.Value.File.Status.ShouldBe(FileStatus.Completed);
        result.Value.DownloadUrl.ToString().ShouldBe(
            $"{FakeBlobStorageService.BaseUrl}/{file.BlobContainer}/{file.BlobPath}?sig=download");
    }

    [Test]
    public async Task ReturnsFileNotFound_WhenFileMissing()
    {
        var result = await TestApp.SendAsync(new GetFileDownloadQuery { FileId = Guid.NewGuid() });

        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(e => e is StorageErrors.FileNotFound);
    }

    [Test]
    public async Task ReturnsBlobNotFound_WhenFileNotCompleted()
    {
        var fileId = Guid.NewGuid();
        await TestApp.AddAsync(NewFile(fileId, FileStatus.Pending));

        var result = await TestApp.SendAsync(new GetFileDownloadQuery { FileId = fileId });

        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(e => e is StorageErrors.BlobNotFound);
    }
}

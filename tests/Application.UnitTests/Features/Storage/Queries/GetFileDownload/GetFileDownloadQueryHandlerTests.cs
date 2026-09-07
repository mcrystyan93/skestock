using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using Shouldly;
using skestock.Application.Common.Errors;
using skestock.Application.Storage.DTOs;
using skestock.Application.Storage.Interfaces;
using skestock.Application.Storage.Queries.GetFileDownload;
using skestock.Application.UnitTests.Features.Storage.Commands.ConfirmUpload;
using skestock.Domain.Entities;
using skestock.Domain.Enums;

namespace skestock.Application.UnitTests.Features.Storage.Queries.GetFileDownload;

public class GetFileDownloadQueryHandlerTests
{
    private static FileMetadataTestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<FileMetadataTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new FileMetadataTestDbContext(options);
    }

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
    public async Task Handle_WhenFileNotInDatabase_ReturnsFileNotFound()
    {
        await using var context = CreateContext();
        var blob = new Mock<IBlobStorageService>();

        var handler = new GetFileDownloadQueryHandler(context, blob.Object);
        var result = await handler.Handle(
            new GetFileDownloadQuery { FileId = Guid.NewGuid() }, CancellationToken.None);

        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(e => e is StorageErrors.FileNotFound);
        blob.Verify(
            b => b.GenerateDownloadSasUriAsync(It.IsAny<GenerateDownloadSasUriDto>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task Handle_WhenFileNotCompleted_ReturnsBlobNotFound()
    {
        await using var context = CreateContext();
        var fileId = Guid.NewGuid();
        context.FileMetadata.Add(NewFile(fileId, FileStatus.Pending));
        await context.SaveChangesAsync(CancellationToken.None);

        var blob = new Mock<IBlobStorageService>();

        var handler = new GetFileDownloadQueryHandler(context, blob.Object);
        var result = await handler.Handle(
            new GetFileDownloadQuery { FileId = fileId }, CancellationToken.None);

        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(e => e is StorageErrors.BlobNotFound);
        blob.Verify(
            b => b.GenerateDownloadSasUriAsync(It.IsAny<GenerateDownloadSasUriDto>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task Handle_WhenFileCompleted_ReturnsMetadataAndDownloadUri()
    {
        await using var context = CreateContext();
        var fileId = Guid.NewGuid();
        var file = NewFile(fileId, FileStatus.Completed);
        context.FileMetadata.Add(file);
        await context.SaveChangesAsync(CancellationToken.None);

        var expectedUri = new Uri("https://acct.blob.core.windows.net/app-files/receipt.pdf?sig=download");
        GenerateDownloadSasUriDto? capturedDto = null;
        var blob = new Mock<IBlobStorageService>();
        blob.Setup(b => b.GenerateDownloadSasUriAsync(It.IsAny<GenerateDownloadSasUriDto>(), It.IsAny<CancellationToken>()))
            .Callback<GenerateDownloadSasUriDto, CancellationToken>((dto, _) => capturedDto = dto)
            .ReturnsAsync(expectedUri);

        var handler = new GetFileDownloadQueryHandler(context, blob.Object);
        var result = await handler.Handle(
            new GetFileDownloadQuery { FileId = fileId }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.DownloadUrl.ShouldBe(expectedUri);
        result.Value.File.FileId.ShouldBe(fileId);
        result.Value.File.OriginalName.ShouldBe(file.OriginalName);
        result.Value.File.Status.ShouldBe(FileStatus.Completed);

        capturedDto.ShouldNotBeNull();
        capturedDto!.ContainerName.ShouldBe(file.BlobContainer);
        capturedDto.BlobPath.ShouldBe(file.BlobPath);
        capturedDto.ValidFor.ShouldBe(TimeSpan.FromMinutes(10));
    }
}

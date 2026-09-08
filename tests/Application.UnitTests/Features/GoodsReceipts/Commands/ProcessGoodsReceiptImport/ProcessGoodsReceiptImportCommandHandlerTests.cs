using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Shouldly;
using skestock.Application.Common.Exceptions;
using skestock.Application.Documents.Interfaces;
using skestock.Application.Features.GoodsReceipts.Commands.ProcessGoodsReceiptImport;
using skestock.Application.Features.GoodsReceipts.Models;
using skestock.Application.Storage.DTOs;
using skestock.Application.Storage.Interfaces;
using skestock.Domain.Entities;
using skestock.Domain.Enums;

namespace skestock.Application.UnitTests.Features.GoodsReceipts.Commands.ProcessGoodsReceiptImport;

public class ProcessGoodsReceiptImportCommandHandlerTests
{
    private static ProcessGoodsReceiptImportTestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ProcessGoodsReceiptImportTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ProcessGoodsReceiptImportTestDbContext(options);
    }

    private static async Task<(ProcessGoodsReceiptImportTestDbContext Context, GoodsReceiptImport Import)>
        CreateContextWithProcessingImportAsync()
    {
        var context = CreateContext();

        var schoolClass = new SchoolClass
        {
            Name = "Fall 2026",
            StartDate = new DateOnly(2026, 9, 1),
            EndDate = new DateOnly(2026, 12, 20)
        };
        context.SchoolClasses.Add(schoolClass);

        var file = new FileMetadata
        {
            FileId = Guid.NewGuid(),
            OriginalName = "receipt.pdf",
            BlobContainer = "app-files",
            BlobPath = "imports/2026/receipt.pdf",
            ContentType = "application/pdf",
            SizeBytes = 1024,
            Status = FileStatus.Completed
        };
        context.FileMetadata.Add(file);

        var import = GoodsReceiptImport.Create(schoolClass.Id, file.Id, Guid.NewGuid(), file.BlobPath);
        import.Class = schoolClass;
        import.FileMetadata = file;
        context.GoodsReceiptImports.Add(import);

        await context.SaveChangesAsync(CancellationToken.None);

        return (context, import);
    }

    [Test]
    public async Task Handle_WhenImportDoesNotExist_ReturnsNotFoundFailure()
    {
        await using var context = CreateContext();
        var blob = new Mock<IBlobStorageService>();
        var extraction = new Mock<IStockDocumentExtractionService>();

        var handler = new ProcessGoodsReceiptImportCommandHandler(
            context, blob.Object, extraction.Object, NullLogger<ProcessGoodsReceiptImportCommandHandler>.Instance);

        var result = await handler.Handle(
            new ProcessGoodsReceiptImportCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsFailed.ShouldBeTrue();
    }

    [Test]
    public async Task Handle_WhenExtractionSucceeds_AppliesResultAndReturnsOk()
    {
        var (context, import) = await CreateContextWithProcessingImportAsync();
        await using var _ = context;

        var blob = new Mock<IBlobStorageService>();
        blob.Setup(b => b.DownloadAsync(It.IsAny<DownloadDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream());

        var extractionResult = new GoodsReceiptExtractionResult { SupplierReference = "PO-123" };
        var extraction = new Mock<IStockDocumentExtractionService>();
        extraction.Setup(e => e.ExtractAsync<GoodsReceiptExtractionResult>(
                It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(extractionResult);

        var handler = new ProcessGoodsReceiptImportCommandHandler(
            context, blob.Object, extraction.Object, NullLogger<ProcessGoodsReceiptImportCommandHandler>.Instance);

        var result = await handler.Handle(
            new ProcessGoodsReceiptImportCommand(import.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();

        var persisted = await context.GoodsReceiptImports.SingleAsync(x => x.Id == import.Id, CancellationToken.None);
        persisted.Status.ShouldBe(GoodsReceiptImportStatus.PendingReview);
        persisted.ExtractedDataJson.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public async Task Handle_WhenExtractionIsUnprocessable_MarksImportFailedAndReturnsOk()
    {
        var (context, import) = await CreateContextWithProcessingImportAsync();
        await using var _ = context;

        var blob = new Mock<IBlobStorageService>();
        blob.Setup(b => b.DownloadAsync(It.IsAny<DownloadDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream());

        var extraction = new Mock<IStockDocumentExtractionService>();
        extraction.Setup(e => e.ExtractAsync<GoodsReceiptExtractionResult>(
                It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnprocessableDocumentException("file is corrupt"));

        var handler = new ProcessGoodsReceiptImportCommandHandler(
            context, blob.Object, extraction.Object, NullLogger<ProcessGoodsReceiptImportCommandHandler>.Instance);

        // permanent failure - the handler absorbs it, marks the import Failed, and returns Ok
        // (there is nothing for the caller to retry).
        var result = await handler.Handle(
            new ProcessGoodsReceiptImportCommand(import.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();

        var persisted = await context.GoodsReceiptImports.SingleAsync(x => x.Id == import.Id, CancellationToken.None);
        persisted.Status.ShouldBe(GoodsReceiptImportStatus.Failed);
        persisted.ErrorMessage.ShouldBe("file is corrupt");
    }

    [Test]
    public async Task Handle_WhenExtractionIsTransientlyUnavailable_PropagatesAndLeavesImportProcessing()
    {
        var (context, import) = await CreateContextWithProcessingImportAsync();
        await using var _ = context;

        var blob = new Mock<IBlobStorageService>();
        blob.Setup(b => b.DownloadAsync(It.IsAny<DownloadDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream());

        var extraction = new Mock<IStockDocumentExtractionService>();
        extraction.Setup(e => e.ExtractAsync<GoodsReceiptExtractionResult>(
                It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TransientExtractionException("network blip"));

        var handler = new ProcessGoodsReceiptImportCommandHandler(
            context, blob.Object, extraction.Object, NullLogger<ProcessGoodsReceiptImportCommandHandler>.Instance);

        // transient failure - the handler must NOT swallow it (that would strand the import as
        // Failed for a retryable error); it propagates so the caller (queue processor) retries.
        var act = async () => await handler.Handle(
            new ProcessGoodsReceiptImportCommand(import.Id), CancellationToken.None);

        await act.ShouldThrowAsync<TransientExtractionException>();

        var persisted = await context.GoodsReceiptImports.SingleAsync(x => x.Id == import.Id, CancellationToken.None);
        persisted.Status.ShouldBe(GoodsReceiptImportStatus.Processing);
    }

    [Test]
    public async Task Handle_WhenImportAlreadyConfirmedOrFailed_IsIdempotentAndReturnsOk()
    {
        var (context, import) = await CreateContextWithProcessingImportAsync();
        await using var _ = context;

        import.MarkAsFailed("already dead");
        await context.SaveChangesAsync(CancellationToken.None);

        var blob = new Mock<IBlobStorageService>();
        var extraction = new Mock<IStockDocumentExtractionService>();

        var handler = new ProcessGoodsReceiptImportCommandHandler(
            context, blob.Object, extraction.Object, NullLogger<ProcessGoodsReceiptImportCommandHandler>.Instance);

        var result = await handler.Handle(
            new ProcessGoodsReceiptImportCommand(import.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        blob.Verify(b => b.DownloadAsync(It.IsAny<DownloadDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}

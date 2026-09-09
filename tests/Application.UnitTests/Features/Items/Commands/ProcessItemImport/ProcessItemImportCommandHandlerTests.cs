using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Shouldly;
using skestock.Application.Common.Exceptions;
using skestock.Application.Documents.Interfaces;
using skestock.Application.Documents.Models;
using skestock.Application.Features.Items.Commands.ProcessItemImport;
using skestock.Application.Storage.DTOs;
using skestock.Application.Storage.Interfaces;
using skestock.Domain.Entities;
using skestock.Domain.Enums;

namespace skestock.Application.UnitTests.Features.Items.Commands.ProcessItemImport;

public class ProcessItemImportCommandHandlerTests
{
    private static ProcessItemImportTestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ProcessItemImportTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ProcessItemImportTestDbContext(options);
    }

    private static async Task<(ProcessItemImportTestDbContext Context, ItemImport Import)>
        CreateContextWithProcessingImportAsync()
    {
        var context = CreateContext();

        var file = new FileMetadata
        {
            FileId = Guid.NewGuid(),
            OriginalName = "items.pdf",
            BlobContainer = "app-files",
            BlobPath = "imports/2026/items.pdf",
            ContentType = "application/pdf",
            SizeBytes = 1024,
            Status = FileStatus.Completed
        };
        context.FileMetadata.Add(file);

        var import = ItemImport.Create(file.Id, Guid.NewGuid(), file.BlobPath);
        import.FileMetadata = file;
        context.ItemImports.Add(import);

        await context.SaveChangesAsync(CancellationToken.None);

        return (context, import);
    }

    [Test]
    public async Task Handle_WhenImportDoesNotExist_ReturnsNotFoundFailure()
    {
        await using var context = CreateContext();
        var blob = new Mock<IBlobStorageService>();
        var extraction = new Mock<IItemDocumentExtractionService>();

        var handler = new ProcessItemImportCommandHandler(
            context, blob.Object, extraction.Object, NullLogger<ProcessItemImportCommandHandler>.Instance);

        var result = await handler.Handle(
            new ProcessItemImportCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsFailed.ShouldBeTrue();
    }

    [Test]
    public async Task Handle_WhenExtractionSucceeds_StoresSuggestionsAndTransitionsToPendingReview()
    {
        var (context, import) = await CreateContextWithProcessingImportAsync();
        await using var _ = context;

        var blob = new Mock<IBlobStorageService>();
        blob.Setup(b => b.DownloadAsync(It.IsAny<DownloadDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream());

        var extractionResult = new ItemExtractionResult
        {
            Items =
            [
                new ExtractedItem { Sku = "SKU-1", Name = "Milk", CategoryName = "Dairy", Unit = "L" },
                new ExtractedItem { Name = "Bread", CategoryName = "Bakery", Unit = "unit" }
            ]
        };
        var extraction = new Mock<IItemDocumentExtractionService>();
        extraction.Setup(e => e.ExtractAsync<ItemExtractionResult>(
                It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(extractionResult);

        var handler = new ProcessItemImportCommandHandler(
            context, blob.Object, extraction.Object, NullLogger<ProcessItemImportCommandHandler>.Instance);

        var result = await handler.Handle(
            new ProcessItemImportCommand(import.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();

        var persisted = await context.ItemImports.SingleAsync(x => x.Id == import.Id, CancellationToken.None);
        persisted.Status.ShouldBe(ItemImportStatus.PendingReview);
        persisted.ExtractedDataJson.ShouldNotBeNullOrEmpty();
        persisted.ExtractedDataJson!.ShouldContain("Milk");
    }

    [Test]
    public async Task Handle_WhenExtractionSucceeds_DoesNotCreateCategories()
    {
        var (context, import) = await CreateContextWithProcessingImportAsync();
        await using var _ = context;

        var blob = new Mock<IBlobStorageService>();
        blob.Setup(b => b.DownloadAsync(It.IsAny<DownloadDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream());

        var extraction = new Mock<IItemDocumentExtractionService>();
        extraction.Setup(e => e.ExtractAsync<ItemExtractionResult>(
                It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ItemExtractionResult
            {
                Items = [new ExtractedItem { Name = "Milk", CategoryName = "Dairy" }]
            });

        var handler = new ProcessItemImportCommandHandler(
            context, blob.Object, extraction.Object, NullLogger<ProcessItemImportCommandHandler>.Instance);

        await handler.Handle(new ProcessItemImportCommand(import.Id), CancellationToken.None);

        // Item is intentionally unmapped in this test context (ProcessItemImport never touches it -
        // it only stores the serialized extraction), so Categories alone proves no side effects.
        (await context.Categories.CountAsync(CancellationToken.None)).ShouldBe(0);
    }

    [Test]
    public async Task Handle_WhenExtractionIsUnprocessable_MarksImportFailedAndReturnsOk()
    {
        var (context, import) = await CreateContextWithProcessingImportAsync();
        await using var _ = context;

        var blob = new Mock<IBlobStorageService>();
        blob.Setup(b => b.DownloadAsync(It.IsAny<DownloadDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream());

        var extraction = new Mock<IItemDocumentExtractionService>();
        extraction.Setup(e => e.ExtractAsync<ItemExtractionResult>(
                It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnprocessableDocumentException("file is corrupt"));

        var handler = new ProcessItemImportCommandHandler(
            context, blob.Object, extraction.Object, NullLogger<ProcessItemImportCommandHandler>.Instance);

        // permanent failure - the handler absorbs it, marks the import Failed, and returns Ok
        // (there is nothing for the caller to retry).
        var result = await handler.Handle(
            new ProcessItemImportCommand(import.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();

        var persisted = await context.ItemImports.SingleAsync(x => x.Id == import.Id, CancellationToken.None);
        persisted.Status.ShouldBe(ItemImportStatus.Failed);
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

        var extraction = new Mock<IItemDocumentExtractionService>();
        extraction.Setup(e => e.ExtractAsync<ItemExtractionResult>(
                It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TransientExtractionException("network blip"));

        var handler = new ProcessItemImportCommandHandler(
            context, blob.Object, extraction.Object, NullLogger<ProcessItemImportCommandHandler>.Instance);

        // transient failure - the handler must NOT swallow it (that would strand the import as
        // Failed for a retryable error); it propagates so the caller (queue processor) retries.
        var act = async () => await handler.Handle(
            new ProcessItemImportCommand(import.Id), CancellationToken.None);

        await act.ShouldThrowAsync<TransientExtractionException>();

        var persisted = await context.ItemImports.SingleAsync(x => x.Id == import.Id, CancellationToken.None);
        persisted.Status.ShouldBe(ItemImportStatus.Processing);
    }

    [Test]
    public async Task Handle_WhenImportAlreadyConfirmedOrFailed_IsIdempotentAndReturnsOk()
    {
        var (context, import) = await CreateContextWithProcessingImportAsync();
        await using var _ = context;

        import.MarkAsFailed("already dead");
        await context.SaveChangesAsync(CancellationToken.None);

        var blob = new Mock<IBlobStorageService>();
        var extraction = new Mock<IItemDocumentExtractionService>();

        var handler = new ProcessItemImportCommandHandler(
            context, blob.Object, extraction.Object, NullLogger<ProcessItemImportCommandHandler>.Instance);

        var result = await handler.Handle(
            new ProcessItemImportCommand(import.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        blob.Verify(b => b.DownloadAsync(It.IsAny<DownloadDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Handle_WhenImportIsPendingReview_IsIdempotentAndDoesNotExtractAgain()
    {
        var (context, import) = await CreateContextWithProcessingImportAsync();
        await using var _ = context;
        import.Status = ItemImportStatus.PendingReview;
        await context.SaveChangesAsync(CancellationToken.None);

        var blob = new Mock<IBlobStorageService>();
        var extraction = new Mock<IItemDocumentExtractionService>();
        var handler = new ProcessItemImportCommandHandler(
            context, blob.Object, extraction.Object, NullLogger<ProcessItemImportCommandHandler>.Instance);

        var result = await handler.Handle(
            new ProcessItemImportCommand(import.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        blob.Verify(b => b.DownloadAsync(It.IsAny<DownloadDto>(), It.IsAny<CancellationToken>()), Times.Never);
        extraction.Verify(e => e.ExtractAsync<ItemExtractionResult>(
            It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}

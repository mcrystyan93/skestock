using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Shouldly;
using skestock.Application.Common.Exceptions;
using skestock.Application.Documents.Interfaces;
using skestock.Application.Documents.Models;
using skestock.Application.Features.Categories.Commands.ProcessCategoryImport;
using skestock.Application.Storage.DTOs;
using skestock.Application.Storage.Interfaces;
using skestock.Domain.Entities;
using skestock.Domain.Enums;

namespace skestock.Application.UnitTests.Features.Categories.Commands.ProcessCategoryImport;

public class ProcessCategoryImportCommandHandlerTests
{
    private static ProcessCategoryImportTestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ProcessCategoryImportTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ProcessCategoryImportTestDbContext(options);
    }

    private static async Task<(ProcessCategoryImportTestDbContext Context, CategoryImport Import)>
        CreateContextWithProcessingImportAsync()
    {
        var context = CreateContext();

        var file = new FileMetadata
        {
            FileId = Guid.NewGuid(),
            OriginalName = "categories.pdf",
            BlobContainer = "app-files",
            BlobPath = "imports/2026/categories.pdf",
            ContentType = "application/pdf",
            SizeBytes = 1024,
            Status = FileStatus.Completed
        };
        context.FileMetadata.Add(file);

        var import = CategoryImport.Create(file.Id, Guid.NewGuid(), file.BlobPath);
        import.FileMetadata = file;
        context.CategoryImports.Add(import);

        await context.SaveChangesAsync(CancellationToken.None);

        return (context, import);
    }

    [Test]
    public async Task Handle_WhenImportDoesNotExist_ReturnsNotFoundFailure()
    {
        await using var context = CreateContext();
        var blob = new Mock<IBlobStorageService>();
        var extraction = new Mock<ICategoryDocumentExtractionService>();

        var handler = new ProcessCategoryImportCommandHandler(
            context, blob.Object, extraction.Object, NullLogger<ProcessCategoryImportCommandHandler>.Instance);

        var result = await handler.Handle(
            new ProcessCategoryImportCommand(Guid.NewGuid()), CancellationToken.None);

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

        var extractionResult = new CategoryExtractionResult
        {
            Categories = [new ExtractedCategory { Name = "Dairy" }, new ExtractedCategory { Name = "Bakery" }]
        };
        var extraction = new Mock<ICategoryDocumentExtractionService>();
        extraction.Setup(e => e.ExtractAsync<CategoryExtractionResult>(
                It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(extractionResult);

        var handler = new ProcessCategoryImportCommandHandler(
            context, blob.Object, extraction.Object, NullLogger<ProcessCategoryImportCommandHandler>.Instance);

        var result = await handler.Handle(
            new ProcessCategoryImportCommand(import.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();

        var persisted = await context.CategoryImports.SingleAsync(x => x.Id == import.Id, CancellationToken.None);
        persisted.Status.ShouldBe(CategoryImportStatus.PendingReview);
        persisted.ExtractedDataJson.ShouldNotBeNullOrEmpty();
        persisted.ExtractedDataJson!.ShouldContain("Dairy");
    }

    [Test]
    public async Task Handle_WhenExtractionSucceeds_DoesNotCreateCategories()
    {
        var (context, import) = await CreateContextWithProcessingImportAsync();
        await using var _ = context;

        var blob = new Mock<IBlobStorageService>();
        blob.Setup(b => b.DownloadAsync(It.IsAny<DownloadDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream());

        var extraction = new Mock<ICategoryDocumentExtractionService>();
        extraction.Setup(e => e.ExtractAsync<CategoryExtractionResult>(
                It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CategoryExtractionResult { Categories = [new ExtractedCategory { Name = "Dairy" }] });

        var handler = new ProcessCategoryImportCommandHandler(
            context, blob.Object, extraction.Object, NullLogger<ProcessCategoryImportCommandHandler>.Instance);

        await handler.Handle(new ProcessCategoryImportCommand(import.Id), CancellationToken.None);

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

        var extraction = new Mock<ICategoryDocumentExtractionService>();
        extraction.Setup(e => e.ExtractAsync<CategoryExtractionResult>(
                It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnprocessableDocumentException("file is corrupt"));

        var handler = new ProcessCategoryImportCommandHandler(
            context, blob.Object, extraction.Object, NullLogger<ProcessCategoryImportCommandHandler>.Instance);

        // permanent failure - the handler absorbs it, marks the import Failed, and returns Ok
        // (there is nothing for the caller to retry).
        var result = await handler.Handle(
            new ProcessCategoryImportCommand(import.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();

        var persisted = await context.CategoryImports.SingleAsync(x => x.Id == import.Id, CancellationToken.None);
        persisted.Status.ShouldBe(CategoryImportStatus.Failed);
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

        var extraction = new Mock<ICategoryDocumentExtractionService>();
        extraction.Setup(e => e.ExtractAsync<CategoryExtractionResult>(
                It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TransientExtractionException("network blip"));

        var handler = new ProcessCategoryImportCommandHandler(
            context, blob.Object, extraction.Object, NullLogger<ProcessCategoryImportCommandHandler>.Instance);

        // transient failure - the handler must NOT swallow it (that would strand the import as
        // Failed for a retryable error); it propagates so the caller (queue processor) retries.
        var act = async () => await handler.Handle(
            new ProcessCategoryImportCommand(import.Id), CancellationToken.None);

        await act.ShouldThrowAsync<TransientExtractionException>();

        var persisted = await context.CategoryImports.SingleAsync(x => x.Id == import.Id, CancellationToken.None);
        persisted.Status.ShouldBe(CategoryImportStatus.Processing);
    }

    [Test]
    public async Task Handle_WhenImportAlreadyConfirmedOrFailed_IsIdempotentAndReturnsOk()
    {
        var (context, import) = await CreateContextWithProcessingImportAsync();
        await using var _ = context;

        import.MarkAsFailed("already dead");
        await context.SaveChangesAsync(CancellationToken.None);

        var blob = new Mock<IBlobStorageService>();
        var extraction = new Mock<ICategoryDocumentExtractionService>();

        var handler = new ProcessCategoryImportCommandHandler(
            context, blob.Object, extraction.Object, NullLogger<ProcessCategoryImportCommandHandler>.Instance);

        var result = await handler.Handle(
            new ProcessCategoryImportCommand(import.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        blob.Verify(b => b.DownloadAsync(It.IsAny<DownloadDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Handle_WhenImportIsPendingReview_IsIdempotentAndDoesNotExtractAgain()
    {
        var (context, import) = await CreateContextWithProcessingImportAsync();
        await using var _ = context;
        import.Status = CategoryImportStatus.PendingReview;
        await context.SaveChangesAsync(CancellationToken.None);

        var blob = new Mock<IBlobStorageService>();
        var extraction = new Mock<ICategoryDocumentExtractionService>();
        var handler = new ProcessCategoryImportCommandHandler(
            context, blob.Object, extraction.Object, NullLogger<ProcessCategoryImportCommandHandler>.Instance);

        var result = await handler.Handle(
            new ProcessCategoryImportCommand(import.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        blob.Verify(b => b.DownloadAsync(It.IsAny<DownloadDto>(), It.IsAny<CancellationToken>()), Times.Never);
        extraction.Verify(e => e.ExtractAsync<CategoryExtractionResult>(
            It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}

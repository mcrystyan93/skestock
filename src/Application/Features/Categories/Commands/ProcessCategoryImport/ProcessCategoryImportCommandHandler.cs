using System.Text.Json;
using Microsoft.Extensions.Logging;
using skestock.Application.Common.Errors;
using skestock.Application.Common.Exceptions;
using skestock.Application.Common.Interfaces;
using skestock.Application.Documents.Interfaces;
using skestock.Application.Documents.Models;
using skestock.Application.Storage.Interfaces;
using skestock.Domain.Enums;

namespace skestock.Application.Features.Categories.Commands.ProcessCategoryImport;

public class ProcessCategoryImportCommandHandler(
    IApplicationDbContext dbContext,
    IBlobStorageService blobStorageService,
    ICategoryDocumentExtractionService categoryDocumentExtractionService,
    ILogger<ProcessCategoryImportCommandHandler> logger) : IRequestHandler<ProcessCategoryImportCommand, Result>
{
    public async ValueTask<Result> Handle(ProcessCategoryImportCommand request, CancellationToken cancellationToken)
    {
        var import = await dbContext.CategoryImports
            .Include(x => x.FileMetadata)
            .FirstOrDefaultAsync(x => x.Id == request.CategoryImportId, cancellationToken);

        if (import is null)
        {
            logger.LogError("Category import not found: {CategoryImportId}", request.CategoryImportId);
            return Result.Fail(new CategoryImportErrors.CategoryImportNotFound(request.CategoryImportId));
        }

        // idempotency at the handler level - it's ok if replayed
        if (import.Status is CategoryImportStatus.PendingReview or CategoryImportStatus.Confirmed or CategoryImportStatus.Failed)
        {
            logger.LogInformation("Category import already processed: {CategoryImportId}, Status: {Status}",
                request.CategoryImportId, import.Status);
            return Result.Ok();
        }

        try
        {
            await using var fileStream =
                await blobStorageService.DownloadAsync(new(import.BlobPath, import.FileMetadata.BlobContainer),
                    cancellationToken);

            var extraction = await categoryDocumentExtractionService.ExtractAsync<CategoryExtractionResult>(fileStream,
                import.FileMetadata.ContentType, cancellationToken);

            // Store the serialized category-name suggestions only - no Category rows are created here;
            // that happens on confirm after the user reviews the suggestions.
            import.ApplyExtractionResult(JsonSerializer.Serialize(extraction));
        }
        catch (UnprocessableDocumentException ex)
        {
            // permanent failure - retrying with the same input would fail identically, so mark
            // the import as Failed here instead of letting the caller retry it.
            logger.LogError(ex, "Unprocessable category import: {CategoryImportId}",
                request.CategoryImportId);

            import.MarkAsFailed(ex.Message);
            await dbContext.SaveChangesAsync(cancellationToken);
            return Result.Ok();
        }
        // Transient failures (e.g. TransientExtractionException) and any other unexpected
        // exception are intentionally left uncaught here so they propagate to the caller
        // (the queue processor), which decides whether to retry.

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}

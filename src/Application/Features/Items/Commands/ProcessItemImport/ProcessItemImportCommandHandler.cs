using System.Text.Json;
using Microsoft.Extensions.Logging;
using skestock.Application.Common.Errors;
using skestock.Application.Common.Exceptions;
using skestock.Application.Common.Interfaces;
using skestock.Application.Documents.Interfaces;
using skestock.Application.Documents.Models;
using skestock.Application.Storage.Interfaces;
using skestock.Domain.Enums;

namespace skestock.Application.Features.Items.Commands.ProcessItemImport;

public class ProcessItemImportCommandHandler(
    IApplicationDbContext dbContext,
    IBlobStorageService blobStorageService,
    IItemDocumentExtractionService itemDocumentExtractionService,
    ILogger<ProcessItemImportCommandHandler> logger) : IRequestHandler<ProcessItemImportCommand, Result>
{
    public async ValueTask<Result> Handle(ProcessItemImportCommand request, CancellationToken cancellationToken)
    {
        var import = await dbContext.ItemImports
            .Include(x => x.FileMetadata)
            .FirstOrDefaultAsync(x => x.Id == request.ItemImportId, cancellationToken);

        if (import is null)
        {
            logger.LogError("Item import not found: {ItemImportId}", request.ItemImportId);
            return Result.Fail(new ItemImportErrors.ItemImportNotFound(request.ItemImportId));
        }

        // idempotency at the handler level - it's ok if replayed
        if (import.Status is ItemImportStatus.PendingReview or ItemImportStatus.Confirmed or ItemImportStatus.Failed)
        {
            logger.LogInformation("Item import already processed: {ItemImportId}, Status: {Status}",
                request.ItemImportId, import.Status);
            return Result.Ok();
        }

        try
        {
            await using var fileStream =
                await blobStorageService.DownloadAsync(new(import.BlobPath, import.FileMetadata.BlobContainer),
                    cancellationToken);

            var extraction = await itemDocumentExtractionService.ExtractAsync<ItemExtractionResult>(fileStream,
                import.FileMetadata.ContentType, cancellationToken);

            // Store the serialized item suggestions only - no Item/Category rows are created here;
            // that happens on confirm after the user reviews the suggestions.
            import.ApplyExtractionResult(JsonSerializer.Serialize(extraction));
        }
        catch (UnprocessableDocumentException ex)
        {
            // permanent failure - retrying with the same input would fail identically, so mark
            // the import as Failed here instead of letting the caller retry it.
            logger.LogError(ex, "Unprocessable item import: {ItemImportId}",
                request.ItemImportId);

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

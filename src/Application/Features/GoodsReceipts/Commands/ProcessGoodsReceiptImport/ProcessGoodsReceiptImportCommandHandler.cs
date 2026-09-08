using Microsoft.Extensions.Logging;
using skestock.Application.Common.Errors;
using skestock.Application.Common.Exceptions;
using skestock.Application.Common.Interfaces;
using skestock.Application.Documents.Interfaces;
using skestock.Application.Features.GoodsReceipts.Models;
using skestock.Application.Storage.Interfaces;
using skestock.Domain.Enums;

namespace skestock.Application.Features.GoodsReceipts.Commands.ProcessGoodsReceiptImport;

public class ProcessGoodsReceiptImportCommandHandler(
    IApplicationDbContext dbContext,
    IBlobStorageService blobStorageService,
    IStockDocumentExtractionService stockDocumentExtractionService,
    ILogger<ProcessGoodsReceiptImportCommandHandler> logger) : IRequestHandler<ProcessGoodsReceiptImportCommand, Result>
{
    public async ValueTask<Result> Handle(ProcessGoodsReceiptImportCommand request, CancellationToken cancellationToken)
    {
        var import = await dbContext.GoodsReceiptImports
            .Include(x => x.FileMetadata)
            .FirstOrDefaultAsync(x => x.Id == request.GoodsReceiptImportId, cancellationToken);

        if (import is null)
        {
            logger.LogError("Goods receipt import not found: {GoodsReceiptImportId}", request.GoodsReceiptImportId);
            return Result.Fail(new GoodsReceiptImportErrors.GoodsReceiptImportNotFound(request.GoodsReceiptImportId));
        }

        // idempotency at the handler level - it's ok if replayed
        if (import.Status is GoodsReceiptImportStatus.Confirmed or GoodsReceiptImportStatus.Failed)
        {
            logger.LogInformation("Goods receipt import already processed: {GoodsReceiptImportId}, Status: {Status}",
                request.GoodsReceiptImportId, import.Status);
            return Result.Ok();
        }

        try
        {
            await using var fileStream =
                await blobStorageService.DownloadAsync(new(import.BlobPath, import.FileMetadata.BlobContainer),
                    cancellationToken);

            var extraction = await stockDocumentExtractionService.ExtractAsync<GoodsReceiptExtractionResult>(fileStream,
                import.FileMetadata.ContentType,  cancellationToken);
            
            import.ApplyExtractionResult(extraction.ToJson());
        }
        catch (UnprocessableDocumentException ex)
        {
            // permanent failure - retrying with the same input would fail identically, so mark
            // the import as Failed here instead of letting the caller retry it.
            logger.LogError(ex, "Unprocessable goods receipt import: {GoodsReceiptImportId}",
                request.GoodsReceiptImportId);

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

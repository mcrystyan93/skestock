using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using skestock.Application.Common.Errors;
using skestock.Application.Common.Exceptions;
using skestock.Application.Common.Interfaces;
using skestock.Application.Common.Models.Options;
using skestock.Application.Documents.Interfaces;
using skestock.Application.Documents.Models;
using skestock.Application.Storage.DTOs;
using skestock.Application.Storage.Interfaces;
using skestock.Domain.Entities;
using skestock.Domain.Enums;

namespace skestock.Application.Features.Items.Commands.ProcessItemImportBatch;

public class ProcessItemImportBatchCommandHandler(
    IApplicationDbContext dbContext,
    IBlobStorageService blobStorageService,
    IItemDocumentExtractionService itemDocumentExtractionService,
    ILogger<ProcessItemImportBatchCommandHandler> logger,
    IOptions<ImportBatchOptions> importBatchOptions) : IRequestHandler<ProcessItemImportBatchCommand, Result>
{
    public async ValueTask<Result> Handle(ProcessItemImportBatchCommand request, CancellationToken cancellationToken)
    {
        var batch = await dbContext.ItemImportBatches
            .Include(b => b.Files).ThenInclude(f => f.FileMetadata)
            .Include(b => b.History)
            .FirstOrDefaultAsync(b => b.Id == request.ItemImportBatchId, cancellationToken);

        if (batch is null)
        {
            logger.LogError("Item import batch not found: {ItemImportBatchId}", request.ItemImportBatchId);
            return Result.Fail(new ItemImportBatchErrors.ItemImportBatchNotFound(request.ItemImportBatchId));
        }

        if (batch.Status is ItemImportBatchStatus.PendingReview or ItemImportBatchStatus.Confirmed or ItemImportBatchStatus.Failed)
        {
            logger.LogInformation("Item import batch already processed: {ItemImportBatchId}, Status: {Status}",
                request.ItemImportBatchId, batch.Status);
            return Result.Ok();
        }

        if (batch.HasActiveProcessingLease(DateTime.UtcNow))
            throw new ImportBatchProcessingInProgressException(batch.Id);

        batch.RecordAttempt(DateTime.UtcNow.AddSeconds(importBatchOptions.Value.ProcessingLeaseSeconds));
        batch.History.Add(ImportBatchHistory.Processing(batch.AttemptCount));
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ImportBatchProcessingInProgressException(batch.Id);
        }

        var orderedFiles = batch.Files.OrderBy(f => f.SortOrder).ToList();
        var streams = new List<Stream>(orderedFiles.Count);

        try
        {
            var inputs = new List<DocumentExtractionInput>(orderedFiles.Count);
            foreach (var file in orderedFiles)
            {
                var stream = await blobStorageService.DownloadAsync(
                    new DownloadDto(file.FileMetadata.BlobPath, file.FileMetadata.BlobContainer), cancellationToken);
                streams.Add(stream);
                inputs.Add(new DocumentExtractionInput(stream, file.FileMetadata.ContentType, file.FileMetadata.OriginalName));
            }

            var extraction = await itemDocumentExtractionService.ExtractAsync<ItemExtractionResult>(
                inputs, cancellationToken);

            batch.ApplyExtractionResult(JsonSerializer.Serialize(extraction));
            batch.History.Add(ImportBatchHistory.Completed(batch.AttemptCount));
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (UnprocessableDocumentException ex)
        {
            logger.LogError(ex, "Unprocessable item import batch: {ItemImportBatchId}",
                request.ItemImportBatchId);

            batch.MarkAsFailed(ex.Message);
            batch.History.Add(ImportBatchHistory.Failed(batch.AttemptCount, ex.Message));
            await dbContext.SaveChangesAsync(cancellationToken);
            return Result.Ok();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            if (batch.AttemptCount >= importBatchOptions.Value.MaxProcessingAttempts)
            {
                logger.LogError(ex,
                    "Exhausted item import batch processing attempts: {ItemImportBatchId}",
                    request.ItemImportBatchId);

                var failureMessage = $"Processing failed after {batch.AttemptCount} attempts: {ex.Message}";
                batch.MarkAsFailed(failureMessage);
                batch.History.Add(ImportBatchHistory.Failed(batch.AttemptCount, failureMessage));
                await dbContext.SaveChangesAsync(cancellationToken);
                return Result.Ok();
            }

            batch.ReleaseProcessingLease();
            await dbContext.SaveChangesAsync(cancellationToken);
            throw;
        }
        finally
        {
            foreach (var stream in streams)
                await stream.DisposeAsync();
        }

        return Result.Ok();
    }
}

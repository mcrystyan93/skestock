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
    IOptions<ImportBatchOptions> importBatchOptions)
    : IRequestHandler<ProcessItemImportBatchCommand, Result>
{
    public async ValueTask<Result> Handle(ProcessItemImportBatchCommand request, CancellationToken cancellationToken)
    {
        var batch = await dbContext.ItemImportBatches
            .Include(importBatch => importBatch.Files).ThenInclude(file => file.FileMetadata)
            .Include(importBatch => importBatch.History)
            .FirstOrDefaultAsync(importBatch => importBatch.Id == request.ItemImportBatchId, cancellationToken);

        if (batch is null)
        {
            logger.LogError("Item import batch not found: {ItemImportBatchId}", request.ItemImportBatchId);
            return Result.Fail(new ItemImportBatchErrors.ItemImportBatchNotFound(request.ItemImportBatchId));
        }

        if (batch.Status is ItemImportBatchStatus.PendingReview or
            ItemImportBatchStatus.Confirmed or ItemImportBatchStatus.Failed)
        {
            logger.LogInformation("Item import batch already processed: {ItemImportBatchId}, Status: {Status}",
                request.ItemImportBatchId, batch.Status);
            return Result.Ok();
        }

        await ClaimProcessingLeaseAsync(batch, cancellationToken);

        var streams = new List<Stream>(batch.Files.Count);
        try
        {
            var inputs = await DownloadInputsAsync(batch, streams, cancellationToken);
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

            await MarkAsFailedAsync(batch, ex.Message, cancellationToken);
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
                await MarkAsFailedAsync(batch, failureMessage, cancellationToken);
                return Result.Ok();
            }

            // Let the queue retry transient failures without waiting for the lease to expire.
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

    private async Task ClaimProcessingLeaseAsync(ItemImportBatch batch, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        if (batch.HasActiveProcessingLease(now))
            throw new ImportBatchProcessingInProgressException(batch.Id);

        batch.RecordAttempt(now.AddSeconds(importBatchOptions.Value.ProcessingLeaseSeconds));
        batch.History.Add(ImportBatchHistory.Processing(batch.AttemptCount));

        // Commit the lease before external I/O so concurrent workers cannot process the same batch.
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ImportBatchProcessingInProgressException(batch.Id);
        }
    }

    private async Task<List<DocumentExtractionInput>> DownloadInputsAsync(
        ItemImportBatch batch, List<Stream> streams, CancellationToken cancellationToken)
    {
        var inputs = new List<DocumentExtractionInput>(batch.Files.Count);
        foreach (var file in batch.Files.OrderBy(file => file.SortOrder))
        {
            var stream = await blobStorageService.DownloadAsync(
                new DownloadDto(file.FileMetadata.BlobPath, file.FileMetadata.BlobContainer),
                cancellationToken);
            streams.Add(stream);
            inputs.Add(new DocumentExtractionInput(
                stream, file.FileMetadata.ContentType, file.FileMetadata.OriginalName));
        }

        return inputs;
    }

    private Task MarkAsFailedAsync(ItemImportBatch batch, string message, CancellationToken cancellationToken)
    {
        batch.MarkAsFailed(message);
        batch.History.Add(ImportBatchHistory.Failed(batch.AttemptCount, message));
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}

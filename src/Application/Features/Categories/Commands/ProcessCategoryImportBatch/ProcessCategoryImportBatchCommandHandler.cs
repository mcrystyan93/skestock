using System.Text.Json;
using Microsoft.EntityFrameworkCore;
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

namespace skestock.Application.Features.Categories.Commands.ProcessCategoryImportBatch;

public class ProcessCategoryImportBatchCommandHandler(
    IApplicationDbContext dbContext,
    IBlobStorageService blobStorageService,
    ICategoryDocumentExtractionService categoryDocumentExtractionService,
    ILogger<ProcessCategoryImportBatchCommandHandler> logger,
    IOptions<ImportBatchOptions> importBatchOptions)
    : IRequestHandler<ProcessCategoryImportBatchCommand, Result>
{
    public async ValueTask<Result> Handle(
        ProcessCategoryImportBatchCommand request, CancellationToken cancellationToken)
    {
        var batch = await dbContext.CategoryImportBatches
            .Include(importBatch => importBatch.Files).ThenInclude(file => file.FileMetadata)
            .Include(importBatch => importBatch.History)
            .FirstOrDefaultAsync(importBatch => importBatch.Id == request.CategoryImportBatchId, cancellationToken);

        if (batch is null)
        {
            logger.LogError("Category import batch not found: {CategoryImportBatchId}",
                request.CategoryImportBatchId);
            return Result.Fail(new CategoryImportBatchErrors.CategoryImportBatchNotFound(request.CategoryImportBatchId));
        }

        if (batch.Status is CategoryImportBatchStatus.PendingReview or
            CategoryImportBatchStatus.Confirmed or CategoryImportBatchStatus.Failed)
        {
            logger.LogInformation("Category import batch already processed: {CategoryImportBatchId}, Status: {Status}",
                request.CategoryImportBatchId, batch.Status);
            return Result.Ok();
        }

        var now = DateTimeOffset.UtcNow;
        if (batch.HasActiveProcessingLease(now))
            throw new ImportBatchProcessingInProgressException(batch.Id);

        batch.RecordAttempt(now.AddSeconds(importBatchOptions.Value.ProcessingLeaseSeconds));
        batch.History.Add(ImportBatchHistory.Processing(batch.AttemptCount));

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ImportBatchProcessingInProgressException(batch.Id);
        }

        var streams = new List<Stream>(batch.Files.Count);
        try
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

            var extraction = await categoryDocumentExtractionService.ExtractAsync<CategoryExtractionResult>(
                inputs, cancellationToken);

            batch.ApplyExtractionResult(JsonSerializer.Serialize(extraction));
            batch.History.Add(ImportBatchHistory.Completed(batch.AttemptCount));
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (UnprocessableDocumentException ex)
        {
            logger.LogError(ex, "Unprocessable category import batch: {CategoryImportBatchId}",
                request.CategoryImportBatchId);
            batch.MarkAsFailed(ex.Message);
            batch.History.Add(ImportBatchHistory.Failed(batch.AttemptCount, ex.Message));
            await dbContext.SaveChangesAsync(cancellationToken);
            return Result.Ok();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            if (batch.AttemptCount >= importBatchOptions.Value.MaxProcessingAttempts)
            {
                logger.LogError(ex, "Exhausted category import batch processing attempts: {CategoryImportBatchId}",
                    request.CategoryImportBatchId);
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

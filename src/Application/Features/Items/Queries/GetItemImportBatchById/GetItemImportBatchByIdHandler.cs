using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Application.Common.Models;
using skestock.Application.Features.Items.Models;

namespace skestock.Application.Features.Items.Queries.GetItemImportBatchById;

public class GetItemImportBatchByIdHandler(IApplicationDbContext dbContext, IUser user)
    : IRequestHandler<GetItemImportBatchByIdQuery, Result<ItemImportBatchReviewDto>>
{
    public async ValueTask<Result<ItemImportBatchReviewDto>> Handle(
        GetItemImportBatchByIdQuery query, CancellationToken cancellationToken)
    {
        if (user.Id is not { } identityId)
            return Result.Fail(new ItemImportBatchErrors.ItemImportBatchNotFound(query.Id));

        var batch = await dbContext.ItemImportBatches
            .AsNoTracking()
            .Include(importBatch => importBatch.Files).ThenInclude(file => file.FileMetadata)
            .Include(importBatch => importBatch.History)
            .Where(importBatch => importBatch.Id == query.Id && importBatch.UploadedByUserId == identityId)
            .SingleOrDefaultAsync(cancellationToken);

        if (batch is null)
            return Result.Fail(new ItemImportBatchErrors.ItemImportBatchNotFound(query.Id));

        // Keep repeated suggestions in their original review order.
        var suggestions = await ItemImportSuggestionResolver.ResolveSuggestionsAsync(
            dbContext, batch.ExtractedDataJson, cancellationToken, deduplicate: false);

        return Result.Ok(new ItemImportBatchReviewDto
        {
            Id = batch.Id,
            Status = batch.Status,
            ClientRequestId = batch.ClientRequestId,
            AttemptCount = batch.AttemptCount,
            ErrorMessage = batch.ErrorMessage,
            UploadedAt = batch.UploadedAt,
            ProcessedAt = batch.ProcessedAt,
            Files = batch.Files
                .OrderBy(file => file.SortOrder)
                .Select(file => new ItemImportBatchFileDto
                {
                    FileMetadataId = file.FileMetadataId,
                    OriginalName = file.FileMetadata.OriginalName,
                    BlobPath = file.FileMetadata.BlobPath,
                    ContentType = file.FileMetadata.ContentType,
                    SizeBytes = file.FileMetadata.SizeBytes,
                    Status = file.FileMetadata.Status,
                    SortOrder = file.SortOrder
                })
                .ToList(),
            History = batch.History
                .OrderBy(history => history.CreatedAtUtc)
                .Select(history => new ImportBatchHistoryDto
                {
                    Status = history.Status,
                    Attempt = history.Attempt,
                    Message = history.Message,
                    CreatedAtUtc = history.CreatedAtUtc
                })
                .ToList(),
            Suggestions = suggestions
        });
    }
}

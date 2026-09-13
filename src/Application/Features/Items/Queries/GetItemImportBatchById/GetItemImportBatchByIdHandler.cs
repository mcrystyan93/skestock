using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Application.Common.Models;
using skestock.Application.Features.Items.Models;

namespace skestock.Application.Features.Items.Queries.GetItemImportBatchById;

public class GetItemImportBatchByIdHandler(IApplicationDbContext dbContext, IUser user)
    : IRequestHandler<GetItemImportBatchByIdQuery, Result<ItemImportBatchReviewDto>>
{
    public async ValueTask<Result<ItemImportBatchReviewDto>> Handle(GetItemImportBatchByIdQuery query,
        CancellationToken cancellationToken)
    {
        if (user.Id is not { } identityId)
            return Result.Fail(new ItemImportBatchErrors.ItemImportBatchNotFound(query.Id));

        var batch = await dbContext.ItemImportBatches
            .AsNoTracking()
            .Include(b => b.Files).ThenInclude(f => f.FileMetadata)
            .Include(b => b.History)
            .Where(b => b.Id == query.Id && b.UploadedByUserId == identityId)
            .SingleOrDefaultAsync(cancellationToken);

        if (batch is null)
            return Result.Fail(new ItemImportBatchErrors.ItemImportBatchNotFound(query.Id));

        var suggestions = await ItemImportSuggestionResolver.ResolveSuggestionsAsync(
            dbContext, batch.ExtractedDataJson, cancellationToken, deduplicate: false);

        var dto = new ItemImportBatchReviewDto
        {
            Id = batch.Id,
            Status = batch.Status,
            ClientRequestId = batch.ClientRequestId,
            AttemptCount = batch.AttemptCount,
            ErrorMessage = batch.ErrorMessage,
            UploadedAt = batch.UploadedAt,
            ProcessedAt = batch.ProcessedAt,
            Files = batch.Files
                .OrderBy(f => f.SortOrder)
                .Select(f => new ItemImportBatchFileDto
                {
                    FileMetadataId = f.FileMetadataId,
                    OriginalName = f.FileMetadata.OriginalName,
                    BlobPath = f.FileMetadata.BlobPath,
                    ContentType = f.FileMetadata.ContentType,
                    SizeBytes = f.FileMetadata.SizeBytes,
                    Status = f.FileMetadata.Status,
                    SortOrder = f.SortOrder
                })
                .ToList(),
            History = batch.History
                .OrderBy(h => h.CreatedAtUtc)
                .Select(h => new ImportBatchHistoryDto
                {
                    Status = h.Status,
                    Attempt = h.Attempt,
                    Message = h.Message,
                    CreatedAtUtc = h.CreatedAtUtc
                })
                .ToList(),
            Suggestions = suggestions
        };

        return Result.Ok(dto);
    }
}

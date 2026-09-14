using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Application.Common.Models;
using skestock.Application.Documents.Models;
using skestock.Application.Features.Categories.Models;
using skestock.Domain.Enums;

namespace skestock.Application.Features.Categories.Queries.GetCategoryImportBatchById;

public class GetCategoryImportBatchByIdHandler(IApplicationDbContext dbContext, IUser user)
    : IRequestHandler<GetCategoryImportBatchByIdQuery, Result<CategoryImportBatchReviewDto>>
{
    public async ValueTask<Result<CategoryImportBatchReviewDto>> Handle(
        GetCategoryImportBatchByIdQuery query, CancellationToken cancellationToken)
    {
        if (user.Id is not { } identityId)
            return Result.Fail(new CategoryImportBatchErrors.CategoryImportBatchNotFound(query.Id));

        var batch = await dbContext.CategoryImportBatches
            .AsNoTracking()
            .Include(importBatch => importBatch.Files).ThenInclude(file => file.FileMetadata)
            .Include(importBatch => importBatch.History)
            .Where(importBatch => importBatch.Id == query.Id &&
                                  importBatch.UploadedByUserId == identityId)
            .SingleOrDefaultAsync(cancellationToken);

        if (batch is null)
            return Result.Fail(new CategoryImportBatchErrors.CategoryImportBatchNotFound(query.Id));

        var extraction = string.IsNullOrWhiteSpace(batch.ExtractedDataJson)
            ? new CategoryExtractionResult()
            : JsonSerializer.Deserialize<CategoryExtractionResult>(batch.ExtractedDataJson) ??
              new CategoryExtractionResult();

        var suggestedNames = (extraction.Categories ?? [])
            .Select(category => category.Name?.Trim())
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var matchedCategories = new Dictionary<string, CategoryImportReviewCategoryDto>(
            StringComparer.OrdinalIgnoreCase);

        if (suggestedNames.Count > 0)
        {
            var upperNames = suggestedNames.Select(name => name.ToUpperInvariant()).ToList();
            var matchedQuery = dbContext.Categories.AsNoTracking().AsQueryable();
            matchedQuery = TextSearchCollation.IsSqlServer(dbContext.Database)
                ? matchedQuery.Where(category => upperNames.Contains(
                    EF.Functions.Collate(category.Name.Trim(), TextSearchCollation.AccentInsensitive)))
                : matchedQuery.Where(category => upperNames.Contains(category.Name.Trim().ToUpper()));
            var matched = await matchedQuery
                .Select(category => new CategoryImportReviewCategoryDto
                {
                    Id = category.Id,
                    Name = category.Name
                })
                .ToListAsync(cancellationToken);
            foreach (var category in matched)
                matchedCategories[category.Name.Trim()] = category;
        }

        return Result.Ok(new CategoryImportBatchReviewDto
        {
            Id = batch.Id,
            Status = batch.Status,
            ClientRequestId = batch.ClientRequestId,
            AttemptCount = batch.AttemptCount,
            ErrorMessage = batch.ErrorMessage,
            UploadedAt = batch.UploadedAt,
            ProcessedAt = batch.ProcessedAt,
            Files = batch.Files.OrderBy(file => file.SortOrder).Select(file => new CategoryImportBatchFileDto
            {
                FileMetadataId = file.FileMetadataId,
                OriginalName = file.FileMetadata.OriginalName,
                BlobPath = file.FileMetadata.BlobPath,
                ContentType = file.FileMetadata.ContentType,
                SizeBytes = file.FileMetadata.SizeBytes,
                Status = file.FileMetadata.Status,
                SortOrder = file.SortOrder
            }).ToList(),
            History = batch.History.OrderBy(history => history.CreatedAtUtc)
                .Select(history => new ImportBatchHistoryDto
                {
                    Status = history.Status,
                    Attempt = history.Attempt,
                    Message = history.Message,
                    CreatedAtUtc = history.CreatedAtUtc
                }).ToList(),
            Suggestions = suggestedNames.Select(name => new CategoryImportReviewLineDto
            {
                Name = name,
                AlreadyExists = matchedCategories.TryGetValue(name, out var matchedCategory),
                MatchedCategory = matchedCategory
            }).ToList()
        });
    }
}

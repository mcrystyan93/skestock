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

        var suggestedNames = GetSuggestedNames(batch.ExtractedDataJson);
        var matchedCategories = await FindMatchingCategoriesAsync(suggestedNames, cancellationToken);

        return Result.Ok(new CategoryImportBatchReviewDto
        {
            Id = batch.Id,
            Status = batch.Status,
            ClientRequestId = batch.ClientRequestId,
            AttemptCount = batch.AttemptCount,
            ErrorMessage = batch.ErrorMessage,
            UploadedAt = batch.UploadedAt,
            ProcessedAt = batch.ProcessedAt,
            Files = batch.Files.OrderBy(file => file.SortOrder)
                .Select(file => new CategoryImportBatchFileDto
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

    private static List<string> GetSuggestedNames(string? extractedDataJson)
    {
        var extraction = string.IsNullOrWhiteSpace(extractedDataJson)
            ? new CategoryExtractionResult()
            : JsonSerializer.Deserialize<CategoryExtractionResult>(extractedDataJson) ??
              new CategoryExtractionResult();

        return (extraction.Categories ?? [])
            .Select(category => category.Name?.Trim())
            .OfType<string>()
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private async Task<Dictionary<string, CategoryImportReviewCategoryDto>> FindMatchingCategoriesAsync(
        IReadOnlyCollection<string> suggestedNames, CancellationToken cancellationToken)
    {
        var matchesByName = new Dictionary<string, CategoryImportReviewCategoryDto>(
            StringComparer.OrdinalIgnoreCase);
        if (suggestedNames.Count == 0)
            return matchesByName;

        var upperNames = suggestedNames.Select(name => name.ToUpperInvariant()).ToList();
        var categories = dbContext.Categories.AsNoTracking();
        // SQL Server needs an explicit accent-insensitive collation; the other providers use upper-case comparison.
        var matchingCategories = TextSearchCollation.IsSqlServer(dbContext.Database)
            ? categories.Where(category => upperNames.Contains(
                EF.Functions.Collate(category.Name.Trim(), TextSearchCollation.AccentInsensitive)))
            : categories.Where(category => upperNames.Contains(category.Name.Trim().ToUpper()));

        var matched = await matchingCategories
            .Select(category => new CategoryImportReviewCategoryDto
            {
                Id = category.Id,
                Name = category.Name
            })
            .ToListAsync(cancellationToken);
        foreach (var category in matched)
            matchesByName[category.Name.Trim()] = category;

        return matchesByName;
    }
}

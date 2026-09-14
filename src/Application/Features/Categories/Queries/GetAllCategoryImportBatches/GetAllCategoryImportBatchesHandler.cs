using Microsoft.EntityFrameworkCore;
using skestock.Application.Common.Filtering;
using skestock.Application.Common.Interfaces;
using skestock.Application.Common.Keyset;
using skestock.Application.Common.Models;
using skestock.Application.Features.Categories.Models;
using skestock.Domain.Entities;

namespace skestock.Application.Features.Categories.Queries.GetAllCategoryImportBatches;

public class GetAllCategoryImportBatchesHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetAllCategoryImportBatchesQuery,
        Result<PaginatedResponse<CategoryImportBatchListItemDto>>>
{
    private static readonly IKeysetSortConfiguration<CategoryImportBatch> SortConfiguration =
        new CategoryImportBatchSortConfiguration();
    private static readonly IFilterConfiguration<CategoryImportBatch> FilterConfiguration =
        new CategoryImportBatchFilterConfiguration();

    public async ValueTask<Result<PaginatedResponse<CategoryImportBatchListItemDto>>> Handle(
        GetAllCategoryImportBatchesQuery request, CancellationToken cancellationToken)
    {
        var effectiveSort = DynamicSortBuilder<CategoryImportBatch>.BuildEffectiveSort(
            request.Sort, SortConfiguration);
        var cursorState = CursorCodec<CategoryImportBatch>.Decode(request.Cursor);
        var query = FilterQueryBuilder<CategoryImportBatch>.Apply(
            dbContext.CategoryImportBatches.AsNoTracking(),
            request.Filters,
            FilterConfiguration,
            TextSearchCollation.IsSqlServer(dbContext.Database));

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim();
            query = TextSearchCollation.IsSqlServer(dbContext.Database)
                ? query.Where(batch =>
                    (batch.ErrorMessage != null &&
                     EF.Functions.Collate(batch.ErrorMessage, TextSearchCollation.AccentInsensitive).Contains(term)) ||
                    batch.Files.Any(file =>
                        EF.Functions.Collate(file.FileMetadata.OriginalName, TextSearchCollation.AccentInsensitive).Contains(term) ||
                        EF.Functions.Collate(file.FileMetadata.BlobPath, TextSearchCollation.AccentInsensitive).Contains(term)))
                : query.Where(batch =>
                    (batch.ErrorMessage != null && batch.ErrorMessage.Contains(term)) ||
                    batch.Files.Any(file =>
                        file.FileMetadata.OriginalName.Contains(term) ||
                        file.FileMetadata.BlobPath.Contains(term)));
        }

        if (cursorState?.KeyValues.Count > 0)
        {
            query = KeysetPredicateBuilder<CategoryImportBatch>.ApplyKeysetPredicate(
                query, effectiveSort, cursorState.KeyValues, SortConfiguration);
        }

        var pageSize = Math.Clamp(request.PageSize, 1, PaginationConstants.DEFAULT_PAGE_SIZE);
        var batches = await OrderByBuilder<CategoryImportBatch>.ApplyOrderBy(
                query, effectiveSort, SortConfiguration)
            .Select(batch => new
            {
                CursorItem = batch,
                Data = new CategoryImportBatchListItemDto
                {
                    Id = batch.Id,
                    Status = batch.Status,
                    ErrorMessage = batch.ErrorMessage,
                    UploadedByName = batch.UploadedByUser.FullName,
                    UploadedAt = batch.UploadedAt,
                    ProcessedAt = batch.ProcessedAt,
                    CreatedDate = batch.CreatedDate,
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
                        }).ToList()
                }
            })
            .Take(pageSize + 1)
            .ToListAsync(cancellationToken);

        var hasNextPage = batches.Count > pageSize;
        if (hasNextPage)
            batches.RemoveAt(batches.Count - 1);

        var lastBatch = batches.LastOrDefault()?.CursorItem;
        return Result.Ok(new PaginatedResponse<CategoryImportBatchListItemDto>
        {
            Data = batches.Select(batch => batch.Data).ToList(),
            HasNextPage = hasNextPage,
            NextCursor = lastBatch is not null
                ? CursorCodec<CategoryImportBatch>.Encode(lastBatch, effectiveSort, SortConfiguration)
                : null,
            Sort =
            [
                ..effectiveSort.Select(sort => new PaginationSort
                {
                    Key = char.ToLowerInvariant(sort.Key[0]) + sort.Key[1..],
                    Value = sort.Direction == "desc" ? "descend" : "ascend"
                })
            ]
        });
    }
}

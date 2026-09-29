using skestock.Application.Common.Filtering;
using skestock.Application.Common.Interfaces;
using skestock.Application.Common.Keyset;
using skestock.Application.Common.Models;
using skestock.Application.Features.Items.Models;
using skestock.Domain.Entities;

namespace skestock.Application.Features.Items.Queries.GetAllItemImportBatches;

public sealed class GetAllItemImportBatchesHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetAllItemImportBatchesQuery, Result<PaginatedResponse<ItemImportBatchListItemDto>>>
{
    private static readonly IKeysetSortConfiguration<ItemImportBatch> SortConfiguration =
        new ItemImportBatchSortConfiguration();
    private static readonly IFilterConfiguration<ItemImportBatch> FilterConfiguration =
        new ItemImportBatchFilterConfiguration();

    public async ValueTask<Result<PaginatedResponse<ItemImportBatchListItemDto>>> Handle(
        GetAllItemImportBatchesQuery request,
        CancellationToken cancellationToken)
    {
        var effectiveSort = DynamicSortBuilder<ItemImportBatch>.BuildEffectiveSort(request.Sort, SortConfiguration);
        var cursorState = CursorCodec<ItemImportBatch>.Decode(request.Cursor);
        var useSqlServerCollation = TextSearchCollation.IsSqlServer(dbContext.Database);
        var query = FilterQueryBuilder<ItemImportBatch>.Apply(
            dbContext.ItemImportBatches.AsNoTracking(),
            request.Filters,
            FilterConfiguration,
            useSqlServerCollation);
        query = ApplySearch(query, request.SearchTerm, useSqlServerCollation);

        if (cursorState?.KeyValues.Count > 0)
        {
            query = KeysetPredicateBuilder<ItemImportBatch>.ApplyKeysetPredicate(
                query, effectiveSort, cursorState.KeyValues, SortConfiguration);
        }

        var pageSize = Math.Clamp(request.PageSize, 1, PaginationConstants.DEFAULT_PAGE_SIZE);
        // Fetch one extra row to detect the next page without a count query.
        var batches = await OrderByBuilder<ItemImportBatch>.ApplyOrderBy(query, effectiveSort, SortConfiguration)
            .Select(batch => new
            {
                Cursor = new ItemImportBatchCursor(batch.Id, batch.UploadedAt, batch.CreatedDate, batch.LastModifiedDate),
                Data = new ItemImportBatchListItemDto
                {
                    Id = batch.Id,
                    Status = batch.Status,
                    ErrorMessage = batch.ErrorMessage,
                    UploadedByName = batch.UploadedByUser.FullName,
                    UploadedAt = batch.UploadedAt,
                    ProcessedAt = batch.ProcessedAt,
                    CreatedDate = batch.CreatedDate,
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
                        .ToList()
                }
            })
            .Take(pageSize + 1)
            .ToListAsync(cancellationToken);

        var hasNextPage = batches.Count > pageSize;
        if (hasNextPage)
            batches.RemoveAt(batches.Count - 1);

        var lastBatch = batches.LastOrDefault()?.Cursor;
        return Result.Ok(new PaginatedResponse<ItemImportBatchListItemDto>
        {
            Data = batches.Select(batch => batch.Data).ToList(),
            HasNextPage = hasNextPage,
            NextCursor = lastBatch is not null
                ? CursorCodec<ItemImportBatch>.Encode(lastBatch, effectiveSort)
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

    private static IQueryable<ItemImportBatch> ApplySearch(
        IQueryable<ItemImportBatch> query, string? searchTerm, bool useSqlServerCollation)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return query;

        var term = searchTerm.Trim();
        return useSqlServerCollation
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
}

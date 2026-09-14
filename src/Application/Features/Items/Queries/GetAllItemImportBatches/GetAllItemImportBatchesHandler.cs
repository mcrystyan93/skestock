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

        var query = dbContext.ItemImportBatches
            .AsNoTracking()
            .AsQueryable();

        query = FilterQueryBuilder<ItemImportBatch>.Apply(
            query,
            request.Filters,
            FilterConfiguration,
            TextSearchCollation.IsSqlServer(dbContext.Database));

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim();
            query = TextSearchCollation.IsSqlServer(dbContext.Database)
                ? query.Where(batch =>
                    batch.ErrorMessage != null &&
                    EF.Functions.Collate(batch.ErrorMessage, TextSearchCollation.AccentInsensitive).Contains(term) ||
                    batch.Files.Any(file =>
                        EF.Functions.Collate(file.FileMetadata.OriginalName, TextSearchCollation.AccentInsensitive).Contains(term) ||
                        EF.Functions.Collate(file.FileMetadata.BlobPath, TextSearchCollation.AccentInsensitive).Contains(term)))
                : query.Where(batch =>
                    batch.ErrorMessage != null && batch.ErrorMessage.Contains(term) ||
                    batch.Files.Any(file =>
                        file.FileMetadata.OriginalName.Contains(term) ||
                        file.FileMetadata.BlobPath.Contains(term)));
        }

        if (cursorState?.KeyValues.Count > 0)
        {
            query = KeysetPredicateBuilder<ItemImportBatch>.ApplyKeysetPredicate(
                query,
                effectiveSort,
                cursorState.KeyValues,
                SortConfiguration);
        }

        var pageSize = Math.Clamp(request.PageSize, 1, PaginationConstants.DEFAULT_PAGE_SIZE);
        var batches = await OrderByBuilder<ItemImportBatch>.ApplyOrderBy(query, effectiveSort, SortConfiguration)
            .Select(batch => new
            {
                CursorItem = batch,
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

        var lastBatch = batches.LastOrDefault()?.CursorItem;
        return Result.Ok(new PaginatedResponse<ItemImportBatchListItemDto>
        {
            Data = batches.Select(batch => batch.Data).ToList(),
            HasNextPage = hasNextPage,
            NextCursor = lastBatch is not null
                ? CursorCodec<ItemImportBatch>.Encode(lastBatch, effectiveSort, SortConfiguration)
                : null,
            Sort =
            [
                ..effectiveSort.Select(sort => new PaginationSort
                {
                    Key = MapSortKey(sort.Key),
                    Value = sort.Direction == "desc" ? "descend" : "ascend"
                })
            ]
        });
    }

    private static string MapSortKey(string propertyName) =>
        char.ToLowerInvariant(propertyName[0]) + propertyName[1..];
}

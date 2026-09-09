using skestock.Application.Common.Filtering;
using skestock.Application.Common.Interfaces;
using skestock.Application.Common.Keyset;
using skestock.Application.Common.Models;
using skestock.Application.Features.Items.Models;
using skestock.Domain.Entities;

namespace skestock.Application.Features.Items.Queries.GetAllItemImports;

public class GetAllItemImportsHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetAllItemImportsQuery, Result<PaginatedResponse<ItemImportListItemDto>>>
{
    private static readonly IKeysetSortConfiguration<ItemImport> SortConfiguration = new ItemImportSortConfiguration();
    private static readonly IFilterConfiguration<ItemImport> FilterConfiguration = new ItemImportFilterConfiguration();

    public async ValueTask<Result<PaginatedResponse<ItemImportListItemDto>>> Handle(GetAllItemImportsQuery request,
        CancellationToken cancellationToken)
    {
        var effectiveSort = DynamicSortBuilder<ItemImport>.BuildEffectiveSort(request.Sort, SortConfiguration);
        var cursorState = CursorCodec<ItemImport>.Decode(request.Cursor);

        var query = dbContext.ItemImports
            .AsNoTracking()
            .AsQueryable();

        query = FilterQueryBuilder<ItemImport>.Apply(query, request.Filters, FilterConfiguration);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim();
            query = query.Where(i =>
                i.BlobPath.Contains(term) || (i.ErrorMessage != null && i.ErrorMessage.Contains(term)));
        }

        if (cursorState?.KeyValues.Count > 0)
        {
            query = KeysetPredicateBuilder<ItemImport>.ApplyKeysetPredicate(
                query,
                effectiveSort,
                cursorState.KeyValues,
                SortConfiguration);
        }

        var pageSize = Math.Clamp(request.PageSize, 1, PaginationConstants.DEFAULT_PAGE_SIZE);

        var imports = await OrderByBuilder<ItemImport>.ApplyOrderBy(query, effectiveSort, SortConfiguration)
            .Select(i => new
            {
                CursorItem = i,
                Data = new ItemImportListItemDto
                {
                    Id = i.Id,
                    Status = i.Status,
                    FileMetadataId = i.FileMetadataId,
                    BlobPath = i.BlobPath,
                    ErrorMessage = i.ErrorMessage,
                    UploadedByName = i.UploadedByUser != null ? i.UploadedByUser.FullName : null,
                    UploadedAt = i.UploadedAt,
                    ProcessedAt = i.ProcessedAt,
                    CreatedDate = i.CreatedDate
                }
            })
            .Take(pageSize + 1)
            .ToListAsync(cancellationToken);

        var hasNextPage = imports.Count > pageSize;
        if (hasNextPage)
            imports.RemoveAt(imports.Count - 1);

        var pageItems = imports.Select(i => i.Data).ToList();
        var lastImport = imports.LastOrDefault()?.CursorItem;

        var data = new PaginatedResponse<ItemImportListItemDto>
        {
            Data = pageItems,
            HasNextPage = hasNextPage,
            NextCursor = lastImport is not null
                ? CursorCodec<ItemImport>.Encode(lastImport, effectiveSort, SortConfiguration)
                : null,
            Sort =
            [
                ..effectiveSort.Select(s => new PaginationSort
                {
                    Key = MapSortKey(s.Key),
                    Value = s.Direction == "desc" ? "descend" : "ascend"
                })
            ]
        };

        return Result.Ok(data);
    }

    private static string MapSortKey(string propertyName) => char.ToLowerInvariant(propertyName[0]) + propertyName[1..];
}

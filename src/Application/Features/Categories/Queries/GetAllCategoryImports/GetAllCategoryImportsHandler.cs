using skestock.Application.Common.Filtering;
using skestock.Application.Common.Interfaces;
using skestock.Application.Common.Keyset;
using skestock.Application.Common.Models;
using skestock.Application.Features.Categories.Models;
using skestock.Domain.Entities;

namespace skestock.Application.Features.Categories.Queries.GetAllCategoryImports;

public class GetAllCategoryImportsHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetAllCategoryImportsQuery, Result<PaginatedResponse<CategoryImportListItemDto>>>
{
    private static readonly IKeysetSortConfiguration<CategoryImport> SortConfiguration = new CategoryImportSortConfiguration();
    private static readonly IFilterConfiguration<CategoryImport> FilterConfiguration = new CategoryImportFilterConfiguration();

    public async ValueTask<Result<PaginatedResponse<CategoryImportListItemDto>>> Handle(GetAllCategoryImportsQuery request,
        CancellationToken cancellationToken)
    {
        var effectiveSort = DynamicSortBuilder<CategoryImport>.BuildEffectiveSort(request.Sort, SortConfiguration);
        var cursorState = CursorCodec<CategoryImport>.Decode(request.Cursor);

        var query = dbContext.CategoryImports
            .AsNoTracking()
            .AsQueryable();

        query = FilterQueryBuilder<CategoryImport>.Apply(query, request.Filters, FilterConfiguration);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim();
            query = query.Where(i =>
                i.BlobPath.Contains(term) || (i.ErrorMessage != null && i.ErrorMessage.Contains(term)));
        }

        if (cursorState?.KeyValues.Count > 0)
        {
            query = KeysetPredicateBuilder<CategoryImport>.ApplyKeysetPredicate(
                query,
                effectiveSort,
                cursorState.KeyValues,
                SortConfiguration);
        }

        var pageSize = Math.Clamp(request.PageSize, 1, PaginationConstants.DEFAULT_PAGE_SIZE);

        var imports = await OrderByBuilder<CategoryImport>.ApplyOrderBy(query, effectiveSort, SortConfiguration)
            .Select(i => new
            {
                CursorItem = i,
                Data = new CategoryImportListItemDto
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

        var data = new PaginatedResponse<CategoryImportListItemDto>
        {
            Data = pageItems,
            HasNextPage = hasNextPage,
            NextCursor = lastImport is not null
                ? CursorCodec<CategoryImport>.Encode(lastImport, effectiveSort, SortConfiguration)
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

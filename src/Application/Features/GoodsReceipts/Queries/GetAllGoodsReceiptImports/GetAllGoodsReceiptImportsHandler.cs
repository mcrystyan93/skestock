using skestock.Application.Common.Filtering;
using skestock.Application.Common.Interfaces;
using skestock.Application.Common.Keyset;
using skestock.Application.Common.Models;
using skestock.Application.Features.GoodsReceipts.Models;
using skestock.Domain.Entities;

namespace skestock.Application.Features.GoodsReceipts.Queries.GetAllGoodsReceiptImports;

public class GetAllGoodsReceiptImportsHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetAllGoodsReceiptImportsQuery, Result<PaginatedResponse<GoodsReceiptImportListItemDto>>>
{
    private static readonly IKeysetSortConfiguration<GoodsReceiptImport> SortConfiguration = new GoodsReceiptImportSortConfiguration();
    private static readonly IFilterConfiguration<GoodsReceiptImport> FilterConfiguration = new GoodsReceiptImportFilterConfiguration();

    public async ValueTask<Result<PaginatedResponse<GoodsReceiptImportListItemDto>>> Handle(GetAllGoodsReceiptImportsQuery request,
        CancellationToken cancellationToken)
    {
        var effectiveSort = DynamicSortBuilder<GoodsReceiptImport>.BuildEffectiveSort(request.Sort, SortConfiguration);
        var cursorState = CursorCodec<GoodsReceiptImport>.Decode(request.Cursor);

        var query = dbContext.GoodsReceiptImports
            .AsNoTracking()
            .AsQueryable();

        query = FilterQueryBuilder<GoodsReceiptImport>.Apply(query, request.Filters, FilterConfiguration);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim();
            query = query.Where(i =>
                i.Class.Name.Contains(term) || (i.ErrorMessage != null && i.ErrorMessage.Contains(term)));
        }

        if (cursorState?.KeyValues.Count > 0)
        {
            query = KeysetPredicateBuilder<GoodsReceiptImport>.ApplyKeysetPredicate(
                query,
                effectiveSort,
                cursorState.KeyValues,
                SortConfiguration);
        }

        var pageSize = Math.Clamp(request.PageSize, 1, PaginationConstants.DEFAULT_PAGE_SIZE);

        var imports = await OrderByBuilder<GoodsReceiptImport>.ApplyOrderBy(query, effectiveSort, SortConfiguration)
            .Select(i => new
            {
                CursorItem = i,
                Data = new GoodsReceiptImportListItemDto
                {
                    Id = i.Id,
                    Status = i.Status,
                    ClassId = i.ClassId,
                    ClassName = i.Class.Name,
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

        var data = new PaginatedResponse<GoodsReceiptImportListItemDto>
        {
            Data = pageItems,
            HasNextPage = hasNextPage,
            NextCursor = lastImport is not null
                ? CursorCodec<GoodsReceiptImport>.Encode(lastImport, effectiveSort, SortConfiguration)
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

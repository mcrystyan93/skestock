using skestock.Application.Common.Filtering;
using skestock.Application.Common.Interfaces;
using skestock.Application.Common.Keyset;
using skestock.Application.Common.Models;
using skestock.Application.Features.GoodsReceipts.Models;
using skestock.Domain.Entities;

namespace skestock.Application.Features.GoodsReceipts.Queries.GetAllGoodsReceipts;

public class GetAllGoodsReceiptsHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetAllGoodsReceiptsQuery, Result<PaginatedResponse<GoodsReceiptListItemDto>>>
{
    private static readonly IKeysetSortConfiguration<GoodsReceipt> SortConfiguration = new GoodsReceiptSortConfiguration();
    private static readonly IFilterConfiguration<GoodsReceipt> FilterConfiguration = new GoodsReceiptFilterConfiguration();

    public async ValueTask<Result<PaginatedResponse<GoodsReceiptListItemDto>>> Handle(GetAllGoodsReceiptsQuery request,
        CancellationToken cancellationToken)
    {
        var effectiveSort = DynamicSortBuilder<GoodsReceipt>.BuildEffectiveSort(request.Sort, SortConfiguration);
        var cursorState = CursorCodec<GoodsReceipt>.Decode(request.Cursor);

        var query = dbContext.GoodsReceipts
            .AsNoTracking()
            .AsQueryable();

        query = FilterQueryBuilder<GoodsReceipt>.Apply(query, request.Filters, FilterConfiguration);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim();
            query = query.Where(r =>
                r.Note.Contains(term) || (r.SupplierReference != null && r.SupplierReference.Contains(term)));
        }

        if (cursorState?.KeyValues.Count > 0)
        {
            query = KeysetPredicateBuilder<GoodsReceipt>.ApplyKeysetPredicate(
                query,
                effectiveSort,
                cursorState.KeyValues,
                SortConfiguration);
        }

        var pageSize = Math.Clamp(request.PageSize, 1, PaginationConstants.DEFAULT_PAGE_SIZE);

        var receipts = await OrderByBuilder<GoodsReceipt>.ApplyOrderBy(query, effectiveSort, SortConfiguration)
            .Select(r => new
            {
                CursorItem = r,
                Data = new GoodsReceiptListItemDto
                {
                    Id = r.Id,
                    ClassId = r.ClassId,
                    ClassName = r.Class.Name,
                    ReceivedAt = r.ReceivedAt,
                    SupplierReference = r.SupplierReference,
                    Note = r.Note,
                    LineCount = r.Batches.Count,
                    TotalQuantity = r.Batches.Sum(b => (int?)b.Quantity) ?? 0,
                    TotalAmount = r.TotalAmount,
                    CreatedByName = r.CreatedBy != null ? r.CreatedBy.FullName : null,
                    CreatedDate = r.CreatedDate
                }
            })
            .Take(pageSize + 1)
            .ToListAsync(cancellationToken);

        var hasNextPage = receipts.Count > pageSize;
        if (hasNextPage)
            receipts.RemoveAt(receipts.Count - 1);

        var pageItems = receipts.Select(r => r.Data).ToList();
        var lastReceipt = receipts.LastOrDefault()?.CursorItem;

        var data = new PaginatedResponse<GoodsReceiptListItemDto>
        {
            Data = pageItems,
            HasNextPage = hasNextPage,
            NextCursor = lastReceipt is not null
                ? CursorCodec<GoodsReceipt>.Encode(lastReceipt, effectiveSort, SortConfiguration)
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

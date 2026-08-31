using skestock.Application.Common.Filtering;
using skestock.Application.Common.Interfaces;
using skestock.Application.Common.Keyset;
using skestock.Application.Common.Models;
using skestock.Application.Features.StockBatches.Models;
using skestock.Domain.Entities;

namespace skestock.Application.Features.StockBatches.Queries.GetAllStockBatches;

public class GetAllStockBatchesHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetAllStockBatchesQuery, Result<PaginatedResponse<StockBatchListItemDto>>>
{
    private static readonly IKeysetSortConfiguration<StockBatch> SortConfiguration = new StockBatchSortConfiguration();
    private static readonly IFilterConfiguration<StockBatch> FilterConfiguration = new StockBatchFilterConfiguration();

    public async ValueTask<Result<PaginatedResponse<StockBatchListItemDto>>> Handle(GetAllStockBatchesQuery request,
        CancellationToken cancellationToken)
    {
        var effectiveSort = DynamicSortBuilder<StockBatch>.BuildEffectiveSort(request.Sort, SortConfiguration);
        var cursorState = CursorCodec<StockBatch>.Decode(request.Cursor);

        var query = dbContext.StockBatches
            .AsNoTracking()
            .AsQueryable();

        query = FilterQueryBuilder<StockBatch>.Apply(query, request.Filters, FilterConfiguration);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim();
            query = query.Where(b => b.Item.Name.Contains(term) || b.Location.Name.Contains(term));
        }

        if (cursorState?.KeyValues.Count > 0)
        {
            query = KeysetPredicateBuilder<StockBatch>.ApplyKeysetPredicate(
                query,
                effectiveSort,
                cursorState.KeyValues,
                SortConfiguration);
        }

        var pageSize = Math.Clamp(request.PageSize, 1, PaginationConstants.DEFAULT_PAGE_SIZE);

        var batches = await OrderByBuilder<StockBatch>.ApplyOrderBy(query, effectiveSort, SortConfiguration)
            .Select(b => new
            {
                CursorItem = b,
                Data = new StockBatchListItemDto
                {
                    Id = b.Id,
                    ItemId = b.ItemId,
                    ItemName = b.Item.Name,
                    LocationId = b.LocationId,
                    LocationName = b.Location.Name,
                    GoodsReceiptId = b.GoodsReceiptId,
                    Quantity = b.Quantity,
                    UnitPrice = b.UnitPrice,
                    LineTotal = b.Quantity * b.UnitPrice,
                    ExpiryDate = b.ExpiryDate,
                    ReceivedDate = b.ReceivedDate,
                    CreatedDate = b.CreatedDate
                }
            })
            .Take(pageSize + 1)
            .ToListAsync(cancellationToken);

        var hasNextPage = batches.Count > pageSize;
        if (hasNextPage)
            batches.RemoveAt(batches.Count - 1);

        var pageItems = batches.Select(b => b.Data).ToList();
        var lastBatch = batches.LastOrDefault()?.CursorItem;

        var data = new PaginatedResponse<StockBatchListItemDto>
        {
            Data = pageItems,
            HasNextPage = hasNextPage,
            NextCursor = lastBatch is not null
                ? CursorCodec<StockBatch>.Encode(lastBatch, effectiveSort, SortConfiguration)
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

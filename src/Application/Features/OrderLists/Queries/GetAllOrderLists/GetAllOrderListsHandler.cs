using skestock.Application.Common.Filtering;
using skestock.Application.Common.Interfaces;
using skestock.Application.Common.Keyset;
using skestock.Application.Common.Models;
using skestock.Application.Features.OrderLists.Models;
using skestock.Domain.Entities;

namespace skestock.Application.Features.OrderLists.Queries.GetAllOrderLists;

public class GetAllOrderListsHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetAllOrderListsQuery, Result<PaginatedResponse<OrderListListItemDto>>>
{
    private static readonly IKeysetSortConfiguration<OrderList> SortConfiguration = new OrderListSortConfiguration();
    private static readonly IFilterConfiguration<OrderList> FilterConfiguration = new OrderListFilterConfiguration();

    public async ValueTask<Result<PaginatedResponse<OrderListListItemDto>>> Handle(GetAllOrderListsQuery request,
        CancellationToken cancellationToken)
    {
        var effectiveSort = DynamicSortBuilder<OrderList>.BuildEffectiveSort(request.Sort, SortConfiguration);
        var cursorState = CursorCodec<OrderList>.Decode(request.Cursor);

        var query = dbContext.OrderLists
            .AsNoTracking()
            .AsQueryable();

        query = FilterQueryBuilder<OrderList>.Apply(
            query,
            request.Filters,
            FilterConfiguration,
            TextSearchCollation.IsSqlServer(dbContext.Database));

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim();
            query = TextSearchCollation.IsSqlServer(dbContext.Database)
                ? query.Where(o =>
                    o.Name != null &&
                    EF.Functions.Collate(o.Name, TextSearchCollation.AccentInsensitive).Contains(term))
                : query.Where(o => o.Name != null && o.Name.Contains(term));
        }

        if (cursorState?.KeyValues.Count > 0)
        {
            query = KeysetPredicateBuilder<OrderList>.ApplyKeysetPredicate(
                query,
                effectiveSort,
                cursorState.KeyValues,
                SortConfiguration);
        }

        var pageSize = Math.Clamp(request.PageSize, 1, PaginationConstants.DEFAULT_PAGE_SIZE);

        var orderLists = await OrderByBuilder<OrderList>.ApplyOrderBy(query, effectiveSort, SortConfiguration)
            .Select(o => new
            {
                CursorItem = o,
                Data = new OrderListListItemDto
                {
                    Id = o.Id,
                    ClassId = o.ClassId,
                    ClassName = o.Class != null ? o.Class.Name : null,
                    Name = o.Name,
                    Status = o.Status.ToString(),
                    LineCount = o.Lines.Count,
                    SubmittedAt = o.SubmittedAt,
                    CreatedByName = o.CreatedBy != null ? o.CreatedBy.FullName : null,
                    CreatedDate = o.CreatedDate,
                    LastModifiedDate = o.LastModifiedDate
                }
            })
            .Take(pageSize + 1)
            .ToListAsync(cancellationToken);

        var hasNextPage = orderLists.Count > pageSize;
        if (hasNextPage)
            orderLists.RemoveAt(orderLists.Count - 1);

        var pageItems = orderLists.Select(o => o.Data).ToList();
        var lastItem = orderLists.LastOrDefault()?.CursorItem;

        var data = new PaginatedResponse<OrderListListItemDto>
        {
            Data = pageItems,
            HasNextPage = hasNextPage,
            NextCursor = lastItem is not null
                ? CursorCodec<OrderList>.Encode(lastItem, effectiveSort, SortConfiguration)
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

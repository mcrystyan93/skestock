using skestock.Application.Common.Filtering;
using skestock.Application.Common.Interfaces;
using skestock.Application.Common.Keyset;
using skestock.Application.Common.Models;
using skestock.Application.Features.Items.Models;
using skestock.Domain.Entities;

namespace skestock.Application.Features.Items.Queries.GetAllItems;

public class GetAllItemsHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetAllItemsQuery, Result<PaginatedResponse<ItemDto>>>
{
    private static readonly IKeysetSortConfiguration<Item> SortConfiguration = new ItemSortConfiguration();
    private static readonly IFilterConfiguration<Item> FilterConfiguration = new ItemFilterConfiguration();

    public async ValueTask<Result<PaginatedResponse<ItemDto>>> Handle(GetAllItemsQuery request,
        CancellationToken cancellationToken)
    {
        var effectiveSort = DynamicSortBuilder<Item>.BuildEffectiveSort(request.Sort, SortConfiguration);
        var cursorState = CursorCodec<Item>.Decode(request.Cursor);

        var query = dbContext.Items
            .AsNoTracking()
            .AsQueryable();

        query = FilterQueryBuilder<Item>.Apply(query, request.Filters, FilterConfiguration);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim();
            query = query.Where(i =>
                i.Name.Contains(term) || (i.Sku != null && i.Sku.Contains(term)));
        }

        if (cursorState?.KeyValues.Count > 0)
        {
            query = KeysetPredicateBuilder<Item>.ApplyKeysetPredicate(
                query,
                effectiveSort,
                cursorState.KeyValues,
                SortConfiguration);
        }

        var pageSize = Math.Clamp(request.PageSize, 1, PaginationConstants.DEFAULT_PAGE_SIZE);

        var items = await OrderByBuilder<Item>.ApplyOrderBy(query, effectiveSort, SortConfiguration)
            .Select(i => new
            {
                CursorItem = i,
                Data = new ItemDto
                {
                    Id = i.Id,
                    Sku = i.Sku,
                    Name = i.Name,
                    Description = i.Description,
                    Unit = i.Unit,
                    MinThreshold = i.MinThreshold,
                    IsPerishable = i.IsPerishable,
                    IsActive = i.IsActive,
                    CategoryId = i.CategoryId,
                    CategoryName = i.Category != null ? i.Category.Name : null,
                    CreatedByName = i.CreatedBy != null ? i.CreatedBy.FullName : null,
                    LastModifiedByName = i.LastModifiedBy != null ? i.LastModifiedBy.FullName : null,
                    CreatedDate = i.CreatedDate,
                    LastModifiedDate = i.LastModifiedDate
                }
            })
            .Take(pageSize + 1)
            .ToListAsync(cancellationToken);

        var hasNextPage = items.Count > pageSize;
        if (hasNextPage)
            items.RemoveAt(items.Count - 1);

        var pageItems = items.Select(item => item.Data).ToList();
        var lastItem = items.LastOrDefault()?.CursorItem;

        var data = new PaginatedResponse<ItemDto>
        {
            Data = pageItems,
            HasNextPage = hasNextPage,
            NextCursor = lastItem is not null
                ? CursorCodec<Item>.Encode(lastItem, effectiveSort, SortConfiguration)
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

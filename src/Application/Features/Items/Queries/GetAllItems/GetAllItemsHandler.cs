using skestock.Application.Common.Filtering;
using skestock.Application.Common.Interfaces;
using skestock.Application.Common.Keyset;
using skestock.Application.Common.Models;
using skestock.Application.Features.Items.Models;
using skestock.Domain.Entities;
using skestock.Domain.Entities.Items;

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
        var query = FilterQueryBuilder<Item>.Apply(
            dbContext.Items.AsNoTracking(),
            request.Filters,
            FilterConfiguration,
            TextSearchCollation.IsSqlServer(dbContext.Database));

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim();
            // Name/Sku carry the accent-insensitive collation at the column level (see ItemConfiguration),
            // so the search needs no per-row EF.Functions.Collate and is provider-agnostic.
            query = query.Where(item =>
                item.Name.Contains(term) ||
                (item.Sku != null && item.Sku.Contains(term)));
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
        // Fetch one extra row to detect the next page without a count query.
        var items = await OrderByBuilder<Item>.ApplyOrderBy(query, effectiveSort, SortConfiguration)
            .Select(item => new
            {
                Cursor = new ItemCursor(item.Id, item.Name, item.Sku, item.Unit, item.CreatedDate, item.LastModifiedDate),
                Data = new ItemDto
                {
                    Id = item.Id,
                    Sku = item.Sku,
                    Name = item.Name,
                    Description = item.Description,
                    Unit = item.Unit,
                    MinThreshold = item.MinThreshold,
                    IsPerishable = item.IsPerishable,
                    ShelfLifeDays = item.ShelfLifeDays,
                    IsActive = item.IsActive,
                    CategoryId = item.CategoryId,
                    CategoryName = item.Category != null ? item.Category.Name : null,
                    CreatedByName = item.CreatedBy != null ? item.CreatedBy.FullName : null,
                    LastModifiedByName = item.LastModifiedBy != null ? item.LastModifiedBy.FullName : null,
                    CreatedDate = item.CreatedDate,
                    LastModifiedDate = item.LastModifiedDate
                }
            })
            .Take(pageSize + 1)
            .ToListAsync(cancellationToken);

        var hasNextPage = items.Count > pageSize;
        if (hasNextPage)
            items.RemoveAt(items.Count - 1);

        var lastItem = items.LastOrDefault()?.Cursor;
        return Result.Ok(new PaginatedResponse<ItemDto>
        {
            Data = items.Select(item => item.Data).ToList(),
            HasNextPage = hasNextPage,
            NextCursor = lastItem is not null
                ? CursorCodec<Item>.Encode(lastItem, effectiveSort)
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

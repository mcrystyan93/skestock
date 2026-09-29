using skestock.Application.Common.Filtering;
using skestock.Application.Common.Interfaces;
using skestock.Application.Common.Keyset;
using skestock.Application.Common.Models;
using skestock.Application.Features.SupplyLists.Models;
using skestock.Domain.Entities;

namespace skestock.Application.Features.SupplyLists.Queries.GetAllSupplyLists;

public class GetAllSupplyListsHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetAllSupplyListsQuery, Result<PaginatedResponse<SupplyListListItemDto>>>
{
    private static readonly IKeysetSortConfiguration<SupplyList> SortConfiguration = new SupplyListSortConfiguration();
    private static readonly IFilterConfiguration<SupplyList> FilterConfiguration = new SupplyListFilterConfiguration();

    public async ValueTask<Result<PaginatedResponse<SupplyListListItemDto>>> Handle(GetAllSupplyListsQuery request,
        CancellationToken cancellationToken)
    {
        var effectiveSort = DynamicSortBuilder<SupplyList>.BuildEffectiveSort(request.Sort, SortConfiguration);
        var cursorState = CursorCodec<SupplyList>.Decode(request.Cursor);
        var isSqlServer = TextSearchCollation.IsSqlServer(dbContext.Database);

        var query = dbContext.SupplyLists
            .AsNoTracking()
            .AsQueryable();

        query = FilterQueryBuilder<SupplyList>.Apply(query, request.Filters, FilterConfiguration, isSqlServer);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim();
            query = isSqlServer
                ? query.Where(l => EF.Functions.Collate(l.Name, TextSearchCollation.AccentInsensitive).Contains(term))
                : query.Where(l => l.Name.Contains(term));
        }

        if (cursorState?.KeyValues.Count > 0)
        {
            query = KeysetPredicateBuilder<SupplyList>.ApplyKeysetPredicate(
                query,
                effectiveSort,
                cursorState.KeyValues,
                SortConfiguration);
        }

        var pageSize = Math.Clamp(request.PageSize, 1, PaginationConstants.DEFAULT_PAGE_SIZE);

        var supplyLists = await OrderByBuilder<SupplyList>.ApplyOrderBy(query, effectiveSort, SortConfiguration)
            .Select(l => new
            {
                Cursor = new SupplyListCursor(l.Id, l.Name, l.Frequency, l.CreatedDate, l.LastModifiedDate),
                Data = new SupplyListListItemDto
                {
                    Id = l.Id,
                    Name = l.Name,
                    Frequency = l.Frequency.ToString(),
                    IntervalWeeks = l.IntervalWeeks,
                    IsActive = l.IsActive,
                    LineCount = l.Lines.Count,
                    CreatedDate = l.CreatedDate,
                    LastModifiedDate = l.LastModifiedDate
                }
            })
            .Take(pageSize + 1)
            .ToListAsync(cancellationToken);

        var hasNextPage = supplyLists.Count > pageSize;
        if (hasNextPage)
            supplyLists.RemoveAt(supplyLists.Count - 1);

        var lastItem = supplyLists.LastOrDefault()?.Cursor;

        return Result.Ok(new PaginatedResponse<SupplyListListItemDto>
        {
            Data = supplyLists.Select(l => l.Data).ToList(),
            HasNextPage = hasNextPage,
            NextCursor = lastItem is not null
                ? CursorCodec<SupplyList>.Encode(lastItem, effectiveSort)
                : null,
            Sort =
            [
                ..effectiveSort.Select(s => new PaginationSort
                {
                    Key = char.ToLowerInvariant(s.Key[0]) + s.Key[1..],
                    Value = s.Direction == "desc" ? "descend" : "ascend"
                })
            ]
        });
    }
}

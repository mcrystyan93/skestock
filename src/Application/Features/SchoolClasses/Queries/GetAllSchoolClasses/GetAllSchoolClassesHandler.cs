using skestock.Application.Common.Filtering;
using skestock.Application.Common.Interfaces;
using skestock.Application.Common.Keyset;
using skestock.Application.Common.Models;
using skestock.Application.Features.SchoolClasses.Models;
using skestock.Domain.Entities;

namespace skestock.Application.Features.SchoolClasses.Queries.GetAllSchoolClasses;

public class GetAllSchoolClassesHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetAllSchoolClassesQuery, Result<PaginatedResponse<SchoolClassDto>>>
{
    private static readonly IKeysetSortConfiguration<SchoolClass> SortConfiguration = new SchoolClassSortConfiguration();
    private static readonly IFilterConfiguration<SchoolClass> FilterConfiguration = new SchoolClassFilterConfiguration();

    public async ValueTask<Result<PaginatedResponse<SchoolClassDto>>> Handle(GetAllSchoolClassesQuery request,
        CancellationToken cancellationToken)
    {
        var effectiveSort = DynamicSortBuilder<SchoolClass>.BuildEffectiveSort(request.Sort, SortConfiguration);
        var cursorState = CursorCodec<SchoolClass>.Decode(request.Cursor);

        var query = dbContext.SchoolClasses
            .AsNoTracking()
            .AsQueryable();

        query = FilterQueryBuilder<SchoolClass>.Apply(query, request.Filters, FilterConfiguration);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim();
            query = query.Where(c => c.Name.Contains(term));
        }

        if (cursorState?.KeyValues.Count > 0)
        {
            query = KeysetPredicateBuilder<SchoolClass>.ApplyKeysetPredicate(
                query,
                effectiveSort,
                cursorState.KeyValues,
                SortConfiguration);
        }

        var pageSize = Math.Clamp(request.PageSize, 1, PaginationConstants.DEFAULT_PAGE_SIZE);

        var items = await OrderByBuilder<SchoolClass>.ApplyOrderBy(query, effectiveSort, SortConfiguration)
            .Select(c => new
            {
                CursorItem = c,
                Data = new SchoolClassDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    StartDate = c.StartDate,
                    EndDate = c.EndDate,
                    Status = c.Status,
                    CreatedByName = c.CreatedBy != null ? c.CreatedBy.FullName : null,
                    LastModifiedByName = c.LastModifiedBy != null ? c.LastModifiedBy.FullName : null,
                    CreatedDate = c.CreatedDate,
                    LastModifiedDate = c.LastModifiedDate
                }
            })
            .Take(pageSize + 1)
            .ToListAsync(cancellationToken);

        var hasNextPage = items.Count > pageSize;
        if (hasNextPage)
            items.RemoveAt(items.Count - 1);

        var pageItems = items.Select(item => item.Data).ToList();
        var lastItem = items.LastOrDefault()?.CursorItem;

        var data = new PaginatedResponse<SchoolClassDto>
        {
            Data = pageItems,
            HasNextPage = hasNextPage,
            NextCursor = lastItem is not null
                ? CursorCodec<SchoolClass>.Encode(lastItem, effectiveSort, SortConfiguration)
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

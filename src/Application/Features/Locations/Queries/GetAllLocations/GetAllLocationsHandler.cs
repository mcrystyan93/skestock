using skestock.Application.Common.Filtering;
using skestock.Application.Common.Interfaces;
using skestock.Application.Common.Keyset;
using skestock.Application.Common.Models;
using skestock.Application.Features.Locations.Models;
using skestock.Domain.Entities;

namespace skestock.Application.Features.Locations.Queries.GetAllLocations;

public class GetAllLocationsHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetAllLocationsQuery, Result<PaginatedResponse<LocationDto>>>
{
    private static readonly IKeysetSortConfiguration<Location> SortConfiguration = new LocationSortConfiguration();
    private static readonly IFilterConfiguration<Location> FilterConfiguration = new LocationFilterConfiguration();

    public async ValueTask<Result<PaginatedResponse<LocationDto>>> Handle(GetAllLocationsQuery request,
        CancellationToken cancellationToken)
    {
        var effectiveSort = DynamicSortBuilder<Location>.BuildEffectiveSort(request.Sort, SortConfiguration);
        var cursorState = CursorCodec<Location>.Decode(request.Cursor);

        var query = dbContext.Locations
            .AsNoTracking()
            .AsQueryable();

        query = FilterQueryBuilder<Location>.Apply(query, request.Filters, FilterConfiguration);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim();
            query = query.Where(l =>
                l.Name.Contains(term));
        }

        if (cursorState?.KeyValues.Count > 0)
        {
            query = KeysetPredicateBuilder<Location>.ApplyKeysetPredicate(
                query,
                effectiveSort,
                cursorState.KeyValues,
                SortConfiguration);
        }

        var pageSize = Math.Clamp(request.PageSize, 1, PaginationConstants.DEFAULT_PAGE_SIZE);

        var locations = await OrderByBuilder<Location>.ApplyOrderBy(query, effectiveSort, SortConfiguration)
            .Select(l => new
            {
                CursorItem = l,
                Data = new LocationDto
                {
                    Id = l.Id,
                    Name = l.Name,
                    Type = l.Type,
                    IsDefault = l.IsDefault,
                    ParentLocationId = l.ParentLocationId,
                    ParentLocationName = l.ParentLocation != null ? l.ParentLocation.Name : null,
                    CreatedByName = l.CreatedBy != null ? l.CreatedBy.FullName : null,
                    LastModifiedByName = l.LastModifiedBy != null ? l.LastModifiedBy.FullName : null,
                    CreatedDate = l.CreatedDate,
                    LastModifiedDate = l.LastModifiedDate
                }
            })
            .Take(pageSize + 1)
            .ToListAsync(cancellationToken);

        var hasNextPage = locations.Count > pageSize;
        if (hasNextPage)
            locations.RemoveAt(locations.Count - 1);

        var pageItems = locations.Select(location => location.Data).ToList();
        var lastLocation = locations.LastOrDefault()?.CursorItem;

        var data = new PaginatedResponse<LocationDto>
        {
            Data = pageItems,
            HasNextPage = hasNextPage,
            NextCursor = lastLocation is not null
                ? CursorCodec<Location>.Encode(lastLocation, effectiveSort, SortConfiguration)
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

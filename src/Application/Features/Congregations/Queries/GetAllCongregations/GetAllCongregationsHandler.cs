using skestock.Application.Common.Filtering;
using skestock.Application.Common.Interfaces;
using skestock.Application.Common.Keyset;
using skestock.Application.Common.Models;
using skestock.Application.Features.Congregations.Models;
using skestock.Domain.Entities.Congregations;

namespace skestock.Application.Features.Congregations.Queries.GetAllCongregations;

public class GetAllCongregationsHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetAllCongregationsQuery, Result<PaginatedResponse<CongregationDto>>>
{
    private static readonly IKeysetSortConfiguration<Congregation> SortConfiguration = new CongregationSortConfiguration();
    private static readonly IFilterConfiguration<Congregation> FilterConfiguration = new CongregationFilterConfiguration();

    public async ValueTask<Result<PaginatedResponse<CongregationDto>>> Handle(GetAllCongregationsQuery request,
        CancellationToken cancellationToken)
    {
        var effectiveSort = DynamicSortBuilder<Congregation>.BuildEffectiveSort(request.Sort, SortConfiguration);
        var cursorState = CursorCodec<Congregation>.Decode(request.Cursor);
        var useSqlServerCollation = TextSearchCollation.IsSqlServer(dbContext.Database);
        var query = FilterQueryBuilder<Congregation>.Apply(
            dbContext.Congregations.AsNoTracking(),
            request.Filters,
            FilterConfiguration,
            useSqlServerCollation);
        query = ApplySearch(query, request.SearchTerm, useSqlServerCollation);

        if (cursorState?.KeyValues.Count > 0)
        {
            query = KeysetPredicateBuilder<Congregation>.ApplyKeysetPredicate(
                query, effectiveSort, cursorState.KeyValues, SortConfiguration);
        }

        var pageSize = Math.Clamp(request.PageSize, 1, PaginationConstants.DEFAULT_PAGE_SIZE);
        // Fetch one extra row to determine whether a next page exists without a count query.
        var categories = await OrderByBuilder<Congregation>.ApplyOrderBy(query, effectiveSort, SortConfiguration)
            .Select(congregation => new
            {
                Cursor = new CongregationCursor(congregation.Id, congregation.Name, congregation.CreatedDate, congregation.LastModifiedDate),
                Data = new CongregationDto
                {
                    Id = congregation.Id,
                    Name = congregation.Name,
                    Email = congregation.Email,
                    Coordinator = congregation.Coordinator,
                    CoordinatorPhone = congregation.CoordinatorPhone,
                    DessertContactPerson = congregation.DessertContactPerson,
                    DessertContactPhone = congregation.DessertContactPhone,
                    CanReceiveStudents = congregation.CanReceiveStudents,
                    IsSmallCongregation = congregation.IsSmallCongregation,
                    Message = congregation.Message,
                    CreatedByName = congregation.CreatedBy != null ? congregation.CreatedBy.FullName : null,
                    LastModifiedByName = congregation.LastModifiedBy != null ? congregation.LastModifiedBy.FullName : null,
                    CreatedDate = congregation.CreatedDate,
                    LastModifiedDate = congregation.LastModifiedDate
                }
            })
            .Take(pageSize + 1)
            .ToListAsync(cancellationToken);

        var hasNextPage = categories.Count > pageSize;
        if (hasNextPage)
            categories.RemoveAt(categories.Count - 1);

        var lastCongregation = categories.LastOrDefault()?.Cursor;
        return Result.Ok(new PaginatedResponse<CongregationDto>
        {
            Data = categories.Select(congregation => congregation.Data).ToList(),
            HasNextPage = hasNextPage,
            NextCursor = lastCongregation is not null
                ? CursorCodec<Congregation>.Encode(lastCongregation, effectiveSort)
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

    private static IQueryable<Congregation> ApplySearch(
        IQueryable<Congregation> query, string? searchTerm, bool useSqlServerCollation)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return query;

        var term = searchTerm.Trim();
        return useSqlServerCollation
            ? query.Where(congregation =>
                EF.Functions.Collate(congregation.Name, TextSearchCollation.AccentInsensitive).Contains(term))
            : query.Where(congregation => congregation.Name.Contains(term));
    }
}

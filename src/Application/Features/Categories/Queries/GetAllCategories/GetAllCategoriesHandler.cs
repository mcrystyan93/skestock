using skestock.Application.Common.Filtering;
using skestock.Application.Common.Interfaces;
using skestock.Application.Common.Keyset;
using skestock.Application.Common.Models;
using skestock.Application.Features.Categories.Models;
using skestock.Domain.Entities;
using skestock.Domain.Entities.Categories;

namespace skestock.Application.Features.Categories.Queries.GetAllCategories;

public class GetAllCategoriesHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetAllCategoriesQuery, Result<PaginatedResponse<CategoryDto>>>
{
    private static readonly IKeysetSortConfiguration<Category> SortConfiguration = new CategorySortConfiguration();
    private static readonly IFilterConfiguration<Category> FilterConfiguration = new CategoryFilterConfiguration();

    public async ValueTask<Result<PaginatedResponse<CategoryDto>>> Handle(GetAllCategoriesQuery request,
        CancellationToken cancellationToken)
    {
        var effectiveSort = DynamicSortBuilder<Category>.BuildEffectiveSort(request.Sort, SortConfiguration);
        var cursorState = CursorCodec<Category>.Decode(request.Cursor);
        var useSqlServerCollation = TextSearchCollation.IsSqlServer(dbContext.Database);
        var query = FilterQueryBuilder<Category>.Apply(
            dbContext.Categories.AsNoTracking(),
            request.Filters,
            FilterConfiguration,
            useSqlServerCollation);
        query = ApplySearch(query, request.SearchTerm, useSqlServerCollation);

        if (cursorState?.KeyValues.Count > 0)
        {
            query = KeysetPredicateBuilder<Category>.ApplyKeysetPredicate(
                query, effectiveSort, cursorState.KeyValues, SortConfiguration);
        }

        var pageSize = Math.Clamp(request.PageSize, 1, PaginationConstants.DEFAULT_PAGE_SIZE);
        // Fetch one extra row to determine whether a next page exists without a count query.
        var categories = await OrderByBuilder<Category>.ApplyOrderBy(query, effectiveSort, SortConfiguration)
            .Select(category => new
            {
                Cursor = new CategoryCursor(category.Id, category.Name, category.CreatedDate, category.LastModifiedDate),
                Data = new CategoryDto
                {
                    Id = category.Id,
                    Name = category.Name,
                    ItemCount = category.Items.Count,
                    Icon = category.Icon == null
                        ? null
                        : new CategoryIconDto
                        {
                            Name = category.Icon.Name,
                            FileName = category.Icon.FileName,
                            Path = category.Icon.Path
                        },
                    CreatedByName = category.CreatedBy != null ? category.CreatedBy.FullName : null,
                    LastModifiedByName = category.LastModifiedBy != null ? category.LastModifiedBy.FullName : null,
                    CreatedDate = category.CreatedDate,
                    LastModifiedDate = category.LastModifiedDate
                }
            })
            .Take(pageSize + 1)
            .ToListAsync(cancellationToken);

        var hasNextPage = categories.Count > pageSize;
        if (hasNextPage)
            categories.RemoveAt(categories.Count - 1);

        var lastCategory = categories.LastOrDefault()?.Cursor;
        return Result.Ok(new PaginatedResponse<CategoryDto>
        {
            Data = categories.Select(category => category.Data).ToList(),
            HasNextPage = hasNextPage,
            NextCursor = lastCategory is not null
                ? CursorCodec<Category>.Encode(lastCategory, effectiveSort)
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

    private static IQueryable<Category> ApplySearch(
        IQueryable<Category> query, string? searchTerm, bool useSqlServerCollation)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return query;

        var term = searchTerm.Trim();
        return useSqlServerCollation
            ? query.Where(category =>
                EF.Functions.Collate(category.Name, TextSearchCollation.AccentInsensitive).Contains(term))
            : query.Where(category => category.Name.Contains(term));
    }
}

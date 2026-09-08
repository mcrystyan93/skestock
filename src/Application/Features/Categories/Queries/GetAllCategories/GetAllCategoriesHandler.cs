using skestock.Application.Common.Filtering;
using skestock.Application.Common.Interfaces;
using skestock.Application.Common.Keyset;
using skestock.Application.Common.Models;
using skestock.Application.Features.Categories.Models;
using skestock.Domain.Entities;

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

        var query = dbContext.Categories
            .AsNoTracking()
            .AsQueryable();

        query = FilterQueryBuilder<Category>.Apply(query, request.Filters, FilterConfiguration);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim();
            query = query.Where(c =>
                c.Name.Contains(term));
        }

        if (cursorState?.KeyValues.Count > 0)
        {
            query = KeysetPredicateBuilder<Category>.ApplyKeysetPredicate(
                query,
                effectiveSort,
                cursorState.KeyValues,
                SortConfiguration);
        }

        var pageSize = Math.Clamp(request.PageSize, 1, PaginationConstants.DEFAULT_PAGE_SIZE);

        var categories = await OrderByBuilder<Category>.ApplyOrderBy(query, effectiveSort, SortConfiguration)
            .Select(c => new
            {
                CursorItem = c,
                Data = new CategoryDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Icon = c.Icon == null
                        ? null
                        : new CategoryIconDto
                        {
                            Name = c.Icon.Name,
                            FileName = c.Icon.FileName,
                            Path = c.Icon.Path
                        },
                    CreatedByName = c.CreatedBy!=null ? c.CreatedBy.FullName : null,
                    LastModifiedByName = c.LastModifiedBy!=null ? c.LastModifiedBy.FullName : null,
                    CreatedDate = c.CreatedDate,
                    LastModifiedDate = c.LastModifiedDate
                }
            })
            .Take(pageSize + 1)
            .ToListAsync(cancellationToken);

        var hasNextPage = categories.Count > pageSize;
        if (hasNextPage)
            categories.RemoveAt(categories.Count - 1);

        var pageItems = categories.Select(category => category.Data).ToList();
        var lastCategory = categories.LastOrDefault()?.CursorItem;
        
        var data = new PaginatedResponse<CategoryDto>
        {
            Data = pageItems,
            HasNextPage = hasNextPage,
            NextCursor = lastCategory is not null
                ? CursorCodec<Category>.Encode(lastCategory, effectiveSort, SortConfiguration)
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

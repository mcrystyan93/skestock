using skestock.Application.Common.Caching;
using skestock.Application.Features.Categories.Models;

namespace skestock.Application.Features.Categories.Commands.CreateCategory;

public class CreateCategoryCommand : IRequest<Result<CategoryDto>>, ICacheInvalidation
{
    public string Name { get; init; } = string.Empty;
    public CategoryIconDto? Icon { get; init; }

    // Invalidate every cached GetAllCategories page/filter/sort combination - a new category
    // can affect any of them (default sort, search matches, filters, etc.).
    public IReadOnlyCollection<string> Tags => [CacheConstants.CategoryListTag];
}

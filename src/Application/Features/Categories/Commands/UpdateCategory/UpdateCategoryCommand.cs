using skestock.Application.Common.Caching;
using skestock.Application.Features.Categories.Models;

namespace skestock.Application.Features.Categories.Commands.UpdateCategory;

public class UpdateCategoryCommand : IRequest<Result<CategoryDto>>, ICacheInvalidation
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public CategoryIconDto? Icon { get; init; }

    // Invalidate every cached GetAllCategories page/filter/sort combination - an updated category
    // can affect any of them (default sort, search matches, filters, etc.).
    public IReadOnlyCollection<string> Tags => [CacheConstants.CategoryListTag];
}

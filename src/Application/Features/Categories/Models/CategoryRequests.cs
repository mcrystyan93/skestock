using skestock.Application.Common.Filtering;
using skestock.Application.Common.Models;

namespace skestock.Application.Features.Categories.Models;

public static class CategoryRequests
{
    public class GetAllCategoriesRequest: BasePaginationFilter
    {
        public List<ColumnFilter> Filters { get; init; } = [];
    }
    
    public class CreateCategoryRequest
    {
        public string Name { get; init; } = string.Empty;
        public CategoryIconDto? Icon { get; init; }
    }

    public class UpdateCategoryRequest
    {
        public string Name { get; init; } = string.Empty;
        public CategoryIconDto? Icon { get; init; }
    }
}

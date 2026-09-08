using skestock.Application.Common.Filtering;
using skestock.Application.Common.Models;

namespace skestock.Application.Features.Categories.Models;

public static class CategoryImportRequests
{
    public class CreateCategoryImportRequest
    {
        public Guid FileMetadataId { get; init; }
    }

    public class GetAllCategoryImportsRequest : BasePaginationFilter
    {
        public List<ColumnFilter> Filters { get; init; } = [];
    }

    public class ConfirmCategoryImportRequest
    {
        // The reviewed list of category names. A future UI may edit or omit the AI's suggestions, so
        // this is the source of truth for what gets created - the stored extraction is only a hint.
        public List<string> Names { get; init; } = [];
    }
}

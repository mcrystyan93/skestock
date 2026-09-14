using skestock.Application.Common.Filtering;
using skestock.Application.Common.Models;

namespace skestock.Application.Features.Categories.Models;

public static class CategoryImportBatchRequests
{
    public class CreateCategoryImportBatchRequest
    {
        public List<Guid> FileMetadataIds { get; init; } = [];
        public Guid? ClientRequestId { get; init; }
    }

    public class GetAllCategoryImportBatchesRequest : BasePaginationFilter
    {
        public List<ColumnFilter> Filters { get; init; } = [];
    }

    public class ConfirmCategoryImportBatchRequest
    {
        public List<string> Names { get; init; } = [];
    }
}

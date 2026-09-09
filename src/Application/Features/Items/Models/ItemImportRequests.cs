using skestock.Application.Common.Filtering;
using skestock.Application.Common.Models;

namespace skestock.Application.Features.Items.Models;

public static class ItemImportRequests
{
    public class CreateItemImportRequest
    {
        public Guid FileMetadataId { get; init; }
    }

    public class GetAllItemImportsRequest : BasePaginationFilter
    {
        public List<ColumnFilter> Filters { get; init; } = [];
    }

    public class ConfirmItemImportRequest
    {
        // The reviewed list of items. A future UI may edit or omit the AI's suggestions, so this is
        // the source of truth for what gets created/reused - the stored extraction is only a hint.
        public List<ConfirmItemImportRequestItem> Items { get; init; } = [];
    }

    public class ConfirmItemImportRequestItem
    {
        public string? Sku { get; init; }
        public string Name { get; init; } = string.Empty;
        public string CategoryName { get; init; } = string.Empty;
        public string Unit { get; init; } = "unit";
        public string? Description { get; init; }
        public bool IsPerishable { get; init; }
    }
}

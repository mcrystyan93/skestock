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
        // The reviewed list of items. The selected item IDs are authoritative; the extracted fields
        // are retained for compatibility and review display.
        public List<ConfirmItemImportRequestItem> Items { get; init; } = [];
    }

    public class ConfirmItemImportRequestItem
    {
        public Guid ItemId { get; init; }
        public string? Sku { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Unit { get; init; } = "unit";
        public string? Description { get; init; }
        public bool IsPerishable { get; init; }
    }
}

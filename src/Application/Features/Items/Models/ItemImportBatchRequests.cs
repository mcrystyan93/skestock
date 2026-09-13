using skestock.Application.Common.Filtering;
using skestock.Application.Common.Models;

namespace skestock.Application.Features.Items.Models;

public static class ItemImportBatchRequests
{
    public class GetAllItemImportBatchesRequest : BasePaginationFilter
    {
        public List<ColumnFilter> Filters { get; init; } = [];
    }

    public class CreateItemImportBatchRequest
    {
        public List<Guid> FileMetadataIds { get; init; } = [];

        /// <summary>
        /// Optional client-supplied idempotency key. Retrying the same create request with the same
        /// key (for the same caller) returns the already-created batch instead of creating a
        /// duplicate.
        /// </summary>
        public Guid? ClientRequestId { get; init; }
    }

    public class ConfirmItemImportBatchRequest
    {
        public List<ConfirmItemImportBatchRequestItem> Items { get; init; } = [];
    }

    public class ConfirmItemImportBatchRequestItem
    {
        public Guid ItemId { get; init; }
        public string? Sku { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Unit { get; init; } = "unit";
        public string? Description { get; init; }
        public bool IsPerishable { get; init; }
    }
}

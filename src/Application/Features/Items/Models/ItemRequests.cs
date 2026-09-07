using skestock.Application.Common.Filtering;
using skestock.Application.Common.Models;

namespace skestock.Application.Features.Items.Models;

public static class ItemRequests
{
    public class GetAllItemsRequest: BasePaginationFilter
    {
        public List<ColumnFilter> Filters { get; init; } = [];
    }

    public class CreateItemRequest
    {
        public string? Sku { get; init; }
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }
        public string Unit { get; init; } = "unit";
        public int MinThreshold { get; init; }
        public bool IsPerishable { get; init; }
        public int? ShelfLifeDays { get; init; }
        public Guid CategoryId { get; init; }
    }

    public class EditItemRequest
    {
        public string? Sku { get; init; }
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }
        public string Unit { get; init; } = "unit";
        public int MinThreshold { get; init; }
        public bool IsPerishable { get; init; }
        public int? ShelfLifeDays { get; init; }
        public Guid CategoryId { get; init; }
    }
}

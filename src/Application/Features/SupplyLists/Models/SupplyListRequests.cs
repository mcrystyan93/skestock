using skestock.Application.Common.Models;
using skestock.Domain.Enums;

namespace skestock.Application.Features.SupplyLists.Models;

public static class SupplyListRequests
{
    public class GetAllSupplyListsRequest : BasePaginationFilter
    {
        public List<ColumnFilter> Filters { get; init; } = [];
    }

    public class SupplyListLineRequest
    {
        public Guid ItemId { get; init; }
        public decimal Quantity { get; init; } = 1;
        public string? Unit { get; init; }
        public string? Notes { get; init; }
    }

    public class CreateSupplyListRequest
    {
        public string Name { get; init; } = string.Empty;
        public string? Note { get; init; }
        public SupplyListFrequency Frequency { get; init; }
        public int? IntervalWeeks { get; init; }
        public List<SupplyListLineRequest> Lines { get; init; } = [];
    }

    public class UpdateSupplyListRequest
    {
        public string Name { get; init; } = string.Empty;
        public string? Note { get; init; }
        public SupplyListFrequency Frequency { get; init; }
        public int? IntervalWeeks { get; init; }
        public List<SupplyListLineRequest> Lines { get; init; } = [];
    }
}

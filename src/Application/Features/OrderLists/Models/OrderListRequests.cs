using skestock.Application.Common.Models;

namespace skestock.Application.Features.OrderLists.Models;

public static class OrderListRequests
{
    public class GetAllOrderListsRequest : BasePaginationFilter
    {
        public List<ColumnFilter> Filters { get; init; } = [];
    }

    public class OrderListLineRequest
    {
        public Guid? ItemId { get; init; }
        public string? ProductName { get; init; }
        public decimal Quantity { get; init; }
        public string? Unit { get; init; }
        public string? Notes { get; init; }
    }

    public class CreateOrderListRequest
    {
        public Guid ClassId { get; init; }
        public string? Name { get; init; }
        public string? Note { get; init; }
        public List<OrderListLineRequest> Lines { get; init; } = [];
    }

    public class UpdateOrderListRequest
    {
        public string? Name { get; init; }
        public string? Note { get; init; }
        public List<OrderListLineRequest> Lines { get; init; } = [];
    }
}

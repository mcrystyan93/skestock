using skestock.Application.Common.Filtering;
using skestock.Application.Common.Models;

namespace skestock.Application.Features.StockBatches.Models;

public static class StockBatchRequests
{
    public class GetAllStockBatchesRequest : BasePaginationFilter
    {
        public List<ColumnFilter> Filters { get; init; } = [];
    }

    public class CreateStockBatchRequest
    {
        public int ItemId { get; init; }
        public int LocationId { get; init; }
        public int ReceivedClassId { get; init; }
        public int Quantity { get; init; }
        public DateOnly? ExpiryDate { get; init; }
        public DateOnly ReceivedDate { get; init; }
        public decimal UnitPrice { get; init; }
    }
}

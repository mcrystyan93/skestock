using skestock.Application.Common.Filtering;
using skestock.Application.Common.Models;

namespace skestock.Application.Features.StockBatches.Models;

public static class StockBatchRequests
{
    public class GetAllStockBatchesRequest : BasePaginationFilter
    {
        public List<ColumnFilter> Filters { get; init; } = [];
    }
}

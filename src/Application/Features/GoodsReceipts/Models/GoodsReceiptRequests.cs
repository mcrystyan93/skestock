using skestock.Application.Common.Filtering;
using skestock.Application.Common.Models;

namespace skestock.Application.Features.GoodsReceipts.Models;

public static class GoodsReceiptRequests
{
    public class GetAllGoodsReceiptsRequest : BasePaginationFilter
    {
        public List<ColumnFilter> Filters { get; init; } = [];
    }

    public class CreateGoodsReceiptLineRequest
    {
        public int ItemId { get; init; }
        public int LocationId { get; init; }
        public int Quantity { get; init; }
        public DateOnly? ExpiryDate { get; init; }
    }

    public class CreateGoodsReceiptRequest
    {
        public int ClassId { get; init; }
        public string? SupplierReference { get; init; }
        public string Note { get; init; } = string.Empty;
        public List<CreateGoodsReceiptLineRequest> Lines { get; init; } = [];
    }
}

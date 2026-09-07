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
        public Guid ItemId { get; init; }
        public Guid LocationId { get; init; }
        public int Quantity { get; init; }
        public DateOnly? ExpiryDate { get; init; }
        public decimal UnitPrice { get; init; }
    }

    public class CreateGoodsReceiptRequest
    {
        public Guid ClassId { get; init; }
        public string? SupplierReference { get; init; }
        public string Note { get; init; } = string.Empty;
        public List<CreateGoodsReceiptLineRequest> Lines { get; init; } = [];
    }

    public class CreateGoodsReceiptImportRequest
    {
        public Guid ClassId { get; init; }
        public Guid FileMetadataId { get; init; }
    }

    public class ConfirmGoodsReceiptImportLineRequest
    {
        public Guid? ItemId { get; init; }
        public string? Name { get; init; }
        public string? Sku { get; init; }
        public string? Unit { get; init; }
        public string? CategoryName { get; init; }
        public bool IsPerishable { get; init; }
        public Guid LocationId { get; init; }
        public int Quantity { get; init; }
        public DateOnly? ExpiryDate { get; init; }
        public decimal UnitPrice { get; init; }
        public int SourceLineIndex { get; init; }
    }

    public class ConfirmGoodsReceiptImportRequest
    {
        public string? SupplierReference { get; init; }
        public string Note { get; init; } = string.Empty;
        public List<ConfirmGoodsReceiptImportLineRequest> Lines { get; init; } = [];
    }

    public class GetAllGoodsReceiptImportsRequest : BasePaginationFilter
    {
        public List<ColumnFilter> Filters { get; init; } = [];
    }
}

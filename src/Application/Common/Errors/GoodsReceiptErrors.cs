namespace skestock.Application.Common.Errors;
using Microsoft.AspNetCore.Http;
using skestock.Domain.Enums;

public static class GoodsReceiptErrors
{
    public sealed class GoodsReceiptNotFound : Error
    {
        public const string ErrorCode = "goods_receipts.not_found";

        public GoodsReceiptNotFound(Guid goodsReceiptId) : base($"Goods receipt with id '{goodsReceiptId}' was not found.")
        {
            Metadata.Add(ErrorMetadataKeys.StatusCode, StatusCodes.Status404NotFound);
            Metadata.Add(ErrorMetadataKeys.Title, "Goods receipt not found");
            Metadata.Add(ErrorMetadataKeys.Code, ErrorCode);
            Metadata.Add(ErrorMetadataKeys.Params, new Dictionary<string, object> { ["goodsReceiptId"] = goodsReceiptId });
        }
    }
}

public static class GoodsReceiptImportErrors
{
    public sealed class GoodsReceiptImportNotFound : Error
    {
        public const string ErrorCode = "goods_receipt_imports.not_found";

        public GoodsReceiptImportNotFound(Guid goodsReceiptImportId) : base($"Goods receipt import with id '{goodsReceiptImportId}' was not found.")
        {
            Metadata.Add(ErrorMetadataKeys.StatusCode, StatusCodes.Status404NotFound);
            Metadata.Add(ErrorMetadataKeys.Title, "Goods receipt import not found");
            Metadata.Add(ErrorMetadataKeys.Code, ErrorCode);
            Metadata.Add(ErrorMetadataKeys.Params, new Dictionary<string, object> { ["goodsReceiptImportId"] = goodsReceiptImportId });
        }
    }

    public sealed class GoodsReceiptImportNotInReview : Error
    {
        public const string ErrorCode = "goods_receipt_imports.not_in_review";

        public GoodsReceiptImportNotInReview(Guid goodsReceiptImportId, GoodsReceiptImportStatus status)
            : base($"Goods receipt import with id '{goodsReceiptImportId}' is '{status}' and cannot be confirmed. Only imports pending review can be confirmed.")
        {
            Metadata.Add(ErrorMetadataKeys.StatusCode, StatusCodes.Status409Conflict);
            Metadata.Add(ErrorMetadataKeys.Title, "Goods receipt import not pending review");
            Metadata.Add(ErrorMetadataKeys.Code, ErrorCode);
            Metadata.Add(ErrorMetadataKeys.Params, new Dictionary<string, object>
            {
                ["goodsReceiptImportId"] = goodsReceiptImportId,
                ["status"] = status.ToString()
            });
        }
    }
}

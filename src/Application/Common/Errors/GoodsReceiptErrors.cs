namespace skestock.Application.Common.Errors;
using Microsoft.AspNetCore.Http;

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

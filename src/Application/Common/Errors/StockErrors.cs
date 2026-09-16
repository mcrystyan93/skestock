using Microsoft.AspNetCore.Http;

namespace skestock.Application.Common.Errors;

public static class StockErrors
{
    public sealed class InsufficientQuantity : Error
    {
        public const string ErrorCode = "stock.insufficient_quantity";

        public InsufficientQuantity(Guid itemId, Guid locationId, int requested, int available)
            : base(
                $"Cannot move {requested} units of item '{itemId}' from location '{locationId}'; only {available} are available.")
        {
            Metadata.Add(ErrorMetadataKeys.StatusCode, StatusCodes.Status409Conflict);
            Metadata.Add(ErrorMetadataKeys.Title, "Insufficient stock");
            Metadata.Add(ErrorMetadataKeys.Code, ErrorCode);
            Metadata.Add(ErrorMetadataKeys.Params,
                new Dictionary<string, object>
                {
                    ["itemId"] = itemId,
                    ["locationId"] = locationId,
                    ["requested"] = requested,
                    ["available"] = available
                });
        }
    }
}

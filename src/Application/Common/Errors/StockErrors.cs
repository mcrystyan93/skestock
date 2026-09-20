using Microsoft.AspNetCore.Http;

namespace skestock.Application.Common.Errors;

public static class StockErrors
{
    public sealed class NoExpiredQuantity : Error
    {
        public const string ErrorCode = "stock.no_expired_quantity";

        public NoExpiredQuantity(Guid classId, Guid itemId, Guid locationId)
            : base("There is no expired stock remaining for this item at this location.")
        {
            Metadata.Add(ErrorMetadataKeys.StatusCode, StatusCodes.Status409Conflict);
            Metadata.Add(ErrorMetadataKeys.Title, "No expired stock");
            Metadata.Add(ErrorMetadataKeys.Code, ErrorCode);
            Metadata.Add(ErrorMetadataKeys.Params,
                new Dictionary<string, object>
                {
                    ["classId"] = classId,
                    ["itemId"] = itemId,
                    ["locationId"] = locationId
                });
        }
    }

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

    public sealed class ConcurrencyConflict : Error
    {
        public const string ErrorCode = "stock.concurrency_conflict";

        public ConcurrencyConflict(Guid classId, Guid itemId, Guid locationId)
            : base(
                "The stock for this item was changed by another operation. Please reload and try again.")
        {
            Metadata.Add(ErrorMetadataKeys.StatusCode, StatusCodes.Status409Conflict);
            Metadata.Add(ErrorMetadataKeys.Title, "Stock changed concurrently");
            Metadata.Add(ErrorMetadataKeys.Code, ErrorCode);
            Metadata.Add(ErrorMetadataKeys.Params,
                new Dictionary<string, object>
                {
                    ["classId"] = classId,
                    ["itemId"] = itemId,
                    ["locationId"] = locationId
                });
        }
    }
}

namespace skestock.Application.Common.Errors;

using Microsoft.AspNetCore.Http;
using skestock.Domain.Enums;

public static class ItemErrors
{
    public sealed class ItemNotFound : Error
    {
        public const string ErrorCode = "items.not_found";

        public ItemNotFound(Guid itemId) : base($"Item with id '{itemId}' was not found.")
        {
            Metadata.Add(ErrorMetadataKeys.StatusCode, StatusCodes.Status404NotFound);
            Metadata.Add(ErrorMetadataKeys.Title, "Item not found");
            Metadata.Add(ErrorMetadataKeys.Code, ErrorCode);
            Metadata.Add(ErrorMetadataKeys.Params, new Dictionary<string, object> { ["itemId"] = itemId });
        }
    }
}

public static class ItemImportBatchErrors
{
    public sealed class ItemImportBatchNotFound : Error
    {
        public const string ErrorCode = "item_import_batches.not_found";

        public ItemImportBatchNotFound(Guid batchId)
            : base($"Item import batch with id '{batchId}' was not found.")
        {
            Metadata.Add(ErrorMetadataKeys.StatusCode, StatusCodes.Status404NotFound);
            Metadata.Add(ErrorMetadataKeys.Title, "Item import batch not found");
            Metadata.Add(ErrorMetadataKeys.Code, ErrorCode);
            Metadata.Add(ErrorMetadataKeys.Params, new Dictionary<string, object> { ["batchId"] = batchId });
        }
    }

    public sealed class ItemImportBatchNotInReview : Error
    {
        public const string ErrorCode = "item_import_batches.not_in_review";

        public ItemImportBatchNotInReview(Guid batchId, ItemImportBatchStatus status)
            : base($"Item import batch with id '{batchId}' is '{status}' and cannot be confirmed. Only batches pending review can be confirmed.")
        {
            Metadata.Add(ErrorMetadataKeys.StatusCode, StatusCodes.Status409Conflict);
            Metadata.Add(ErrorMetadataKeys.Title, "Item import batch not pending review");
            Metadata.Add(ErrorMetadataKeys.Code, ErrorCode);
            Metadata.Add(ErrorMetadataKeys.Params, new Dictionary<string, object>
            {
                ["batchId"] = batchId,
                ["status"] = status.ToString()
            });
        }
    }

    public sealed class ItemsNotFoundInBatch : Error
    {
        public const string ErrorCode = "item_import_batches.items_not_found";

        public ItemsNotFoundInBatch(Guid batchId, IReadOnlyCollection<Guid> itemIds)
            : base($"Item import batch '{batchId}' contains item selections that no longer exist.")
        {
            Metadata.Add(ErrorMetadataKeys.StatusCode, StatusCodes.Status400BadRequest);
            Metadata.Add(ErrorMetadataKeys.Title, "Item not found");
            Metadata.Add(ErrorMetadataKeys.Code, ErrorCode);
            Metadata.Add(ErrorMetadataKeys.Params, new Dictionary<string, object>
            {
                ["batchId"] = batchId,
                ["itemIds"] = itemIds
            });
        }
    }

    public sealed class DuplicateItemsInBatch : Error
    {
        public const string ErrorCode = "item_import_batches.duplicate_items";

        public DuplicateItemsInBatch(Guid batchId, IReadOnlyCollection<Guid> itemIds)
            : base($"Item import batch '{batchId}' selects the same item more than once.")
        {
            Metadata.Add(ErrorMetadataKeys.StatusCode, StatusCodes.Status400BadRequest);
            Metadata.Add(ErrorMetadataKeys.Title, "Duplicate item selection");
            Metadata.Add(ErrorMetadataKeys.Code, ErrorCode);
            Metadata.Add(ErrorMetadataKeys.Params, new Dictionary<string, object>
            {
                ["batchId"] = batchId,
                ["itemIds"] = itemIds
            });
        }
    }

    public sealed class IdempotencyConflict : Error
    {
        public const string ErrorCode = "item_import_batches.idempotency_conflict";

        public IdempotencyConflict(Guid clientRequestId)
            : base($"Client request id '{clientRequestId}' was already used for a different item import batch.")
        {
            Metadata.Add(ErrorMetadataKeys.StatusCode, StatusCodes.Status409Conflict);
            Metadata.Add(ErrorMetadataKeys.Title, "Idempotency key conflict");
            Metadata.Add(ErrorMetadataKeys.Code, ErrorCode);
            Metadata.Add(ErrorMetadataKeys.Params, new Dictionary<string, object>
            {
                ["clientRequestId"] = clientRequestId
            });
        }
    }

}

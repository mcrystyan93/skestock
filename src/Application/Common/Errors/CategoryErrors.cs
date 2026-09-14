namespace skestock.Application.Common.Errors;
using Microsoft.AspNetCore.Http;
using skestock.Domain.Enums;

public static class CategoryErrors
{
    public sealed class CategoryNotFound : Error
    {
        public const string ErrorCode = "categories.not_found";

        public CategoryNotFound(Guid categoryId) : base($"Category with id '{categoryId}' was not found.")
        {
            Metadata.Add(ErrorMetadataKeys.StatusCode, StatusCodes.Status404NotFound);
            Metadata.Add(ErrorMetadataKeys.Title, "Category not found");
            Metadata.Add(ErrorMetadataKeys.Code, ErrorCode);
            Metadata.Add(ErrorMetadataKeys.Params, new Dictionary<string, object> { ["categoryId"] = categoryId });
        }
    }
}

public static class CategoryImportBatchErrors
{
    public sealed class IdempotencyConflict : Error
    {
        public const string ErrorCode = "category_import_batches.idempotency_conflict";

        public IdempotencyConflict(Guid clientRequestId)
            : base($"Client request id '{clientRequestId}' was already used for a different category import batch.")
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

    public sealed class CategoryImportBatchNotFound : Error
    {
        public const string ErrorCode = "category_import_batches.not_found";

        public CategoryImportBatchNotFound(Guid batchId)
            : base($"Category import batch with id '{batchId}' was not found.")
        {
            Metadata.Add(ErrorMetadataKeys.StatusCode, StatusCodes.Status404NotFound);
            Metadata.Add(ErrorMetadataKeys.Title, "Category import batch not found");
            Metadata.Add(ErrorMetadataKeys.Code, ErrorCode);
            Metadata.Add(ErrorMetadataKeys.Params, new Dictionary<string, object> { ["batchId"] = batchId });
        }
    }

    public sealed class CategoryImportBatchNotInReview : Error
    {
        public const string ErrorCode = "category_import_batches.not_in_review";

        public CategoryImportBatchNotInReview(Guid batchId, CategoryImportBatchStatus status)
            : base($"Category import batch with id '{batchId}' is '{status}' and cannot be confirmed. Only batches pending review can be confirmed.")
        {
            Metadata.Add(ErrorMetadataKeys.StatusCode, StatusCodes.Status409Conflict);
            Metadata.Add(ErrorMetadataKeys.Title, "Category import batch not pending review");
            Metadata.Add(ErrorMetadataKeys.Code, ErrorCode);
            Metadata.Add(ErrorMetadataKeys.Params, new Dictionary<string, object>
            {
                ["batchId"] = batchId,
                ["status"] = status.ToString()
            });
        }
    }
}

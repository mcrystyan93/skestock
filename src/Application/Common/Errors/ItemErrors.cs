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

public static class ItemImportErrors
{
    public sealed class FileMetadataNotConfirmed : Error
    {
        public const string ErrorCode = "item_imports.file_not_confirmed";

        public FileMetadataNotConfirmed(Guid fileMetadataId)
            : base($"File metadata with id '{fileMetadataId}' was not found or is not confirmed.")
        {
            Metadata.Add(ErrorMetadataKeys.StatusCode, StatusCodes.Status409Conflict);
            Metadata.Add(ErrorMetadataKeys.Title, "File upload not confirmed");
            Metadata.Add(ErrorMetadataKeys.Code, ErrorCode);
            Metadata.Add(ErrorMetadataKeys.Params,
                new Dictionary<string, object> { ["fileMetadataId"] = fileMetadataId });
        }
    }

    public sealed class ItemImportNotFound : Error
    {
        public const string ErrorCode = "item_imports.not_found";

        public ItemImportNotFound(Guid itemImportId) : base($"Item import with id '{itemImportId}' was not found.")
        {
            Metadata.Add(ErrorMetadataKeys.StatusCode, StatusCodes.Status404NotFound);
            Metadata.Add(ErrorMetadataKeys.Title, "Item import not found");
            Metadata.Add(ErrorMetadataKeys.Code, ErrorCode);
            Metadata.Add(ErrorMetadataKeys.Params, new Dictionary<string, object> { ["itemImportId"] = itemImportId });
        }
    }

    public sealed class ItemImportNotInReview : Error
    {
        public const string ErrorCode = "item_imports.not_in_review";

        public ItemImportNotInReview(Guid itemImportId, ItemImportStatus status)
            : base(
                $"Item import with id '{itemImportId}' is '{status}' and cannot be confirmed. Only imports pending review can be confirmed.")
        {
            Metadata.Add(ErrorMetadataKeys.StatusCode, StatusCodes.Status409Conflict);
            Metadata.Add(ErrorMetadataKeys.Title, "Item import not pending review");
            Metadata.Add(ErrorMetadataKeys.Code, ErrorCode);
            Metadata.Add(ErrorMetadataKeys.Params,
                new Dictionary<string, object> { ["itemImportId"] = itemImportId, ["status"] = status.ToString() });
        }
    }

    public sealed class RowsWithoutCategory : Error
    {
        public const string ErrorCode = "item_imports.rows_without_category";

        public RowsWithoutCategory(Guid itemImportId)
            : base($"Item import '{itemImportId}' contains one or more rows without a category.")
        {
            Metadata.Add(ErrorMetadataKeys.StatusCode, StatusCodes.Status400BadRequest);
            Metadata.Add(ErrorMetadataKeys.Title, "Category is required");
            Metadata.Add(ErrorMetadataKeys.Code, ErrorCode);
            Metadata.Add(ErrorMetadataKeys.Params, new Dictionary<string, object> { ["itemImportId"] = itemImportId });
        }
    }
}

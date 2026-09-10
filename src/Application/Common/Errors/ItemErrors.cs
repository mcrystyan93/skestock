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

    public sealed class ItemsNotFound : Error
    {
        public const string ErrorCode = "item_imports.items_not_found";

        public ItemsNotFound(Guid itemImportId, IReadOnlyCollection<Guid> itemIds)
            : base($"Item import '{itemImportId}' contains item selections that no longer exist.")
        {
            Metadata.Add(ErrorMetadataKeys.StatusCode, StatusCodes.Status400BadRequest);
            Metadata.Add(ErrorMetadataKeys.Title, "Item not found");
            Metadata.Add(ErrorMetadataKeys.Code, ErrorCode);
            Metadata.Add(ErrorMetadataKeys.Params, new Dictionary<string, object>
            {
                ["itemImportId"] = itemImportId,
                ["itemIds"] = itemIds
            });
        }
    }

    public sealed class DuplicateItems : Error
    {
        public const string ErrorCode = "item_imports.duplicate_items";

        public DuplicateItems(Guid itemImportId, IReadOnlyCollection<Guid> itemIds)
            : base($"Item import '{itemImportId}' selects the same item more than once.")
        {
            Metadata.Add(ErrorMetadataKeys.StatusCode, StatusCodes.Status400BadRequest);
            Metadata.Add(ErrorMetadataKeys.Title, "Duplicate item selection");
            Metadata.Add(ErrorMetadataKeys.Code, ErrorCode);
            Metadata.Add(ErrorMetadataKeys.Params, new Dictionary<string, object>
            {
                ["itemImportId"] = itemImportId,
                ["itemIds"] = itemIds
            });
        }
    }

}

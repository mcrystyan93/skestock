namespace skestock.Application.Common.Errors;
using Microsoft.AspNetCore.Http;

public static class SupplyListErrors
{
    public sealed class SupplyListNotFound : Error
    {
        public const string ErrorCode = "supply_lists.not_found";

        public SupplyListNotFound(Guid supplyListId) : base($"Supply list with id '{supplyListId}' was not found.")
        {
            Metadata.Add(ErrorMetadataKeys.StatusCode, StatusCodes.Status404NotFound);
            Metadata.Add(ErrorMetadataKeys.Title, "Supply list not found");
            Metadata.Add(ErrorMetadataKeys.Code, ErrorCode);
            Metadata.Add(ErrorMetadataKeys.Params, new Dictionary<string, object> { ["supplyListId"] = supplyListId });
        }
    }

    // A disabled list is read-only until it is enabled again.
    public sealed class SupplyListNotEditable : Error
    {
        public const string ErrorCode = "supply_lists.not_editable";

        public SupplyListNotEditable(Guid supplyListId) : base(
            $"Supply list '{supplyListId}' is disabled and cannot be modified.")
        {
            Metadata.Add(ErrorMetadataKeys.StatusCode, StatusCodes.Status409Conflict);
            Metadata.Add(ErrorMetadataKeys.Title, "Supply list not editable");
            Metadata.Add(ErrorMetadataKeys.Code, ErrorCode);
            Metadata.Add(ErrorMetadataKeys.Params, new Dictionary<string, object> { ["supplyListId"] = supplyListId });
        }
    }

    public sealed class SupplyListItemInactive : Error
    {
        public const string ErrorCode = "supply_lists.item_inactive";

        public SupplyListItemInactive(Guid itemId) : base(
            $"Item '{itemId}' is not active and cannot be added to a supply list.")
        {
            Metadata.Add(ErrorMetadataKeys.StatusCode, StatusCodes.Status400BadRequest);
            Metadata.Add(ErrorMetadataKeys.Title, "Item is not active");
            Metadata.Add(ErrorMetadataKeys.Code, ErrorCode);
            Metadata.Add(ErrorMetadataKeys.Params, new Dictionary<string, object> { ["itemId"] = itemId });
        }
    }
}

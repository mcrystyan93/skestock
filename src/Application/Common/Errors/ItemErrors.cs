namespace skestock.Application.Common.Errors;
using Microsoft.AspNetCore.Http;

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

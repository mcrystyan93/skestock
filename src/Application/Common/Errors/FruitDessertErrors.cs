using Microsoft.AspNetCore.Http;

namespace skestock.Application.Common.Errors;

public static class FruitDessertErrors
{
    public sealed class EntryNotFound : Error
    {
        public const string ErrorCode = "fruitDessert.entry_not_found";

        public EntryNotFound(Guid entryId)
            : base($"Fruit and dessert plan entry '{entryId}' was not found.")
        {
            Metadata.Add(ErrorMetadataKeys.StatusCode, StatusCodes.Status404NotFound);
            Metadata.Add(ErrorMetadataKeys.Title, "Plan entry not found");
            Metadata.Add(ErrorMetadataKeys.Code, ErrorCode);
            Metadata.Add(ErrorMetadataKeys.Params, new Dictionary<string, object> { ["entryId"] = entryId });
        }
    }

    public sealed class ContactMissing : Error
    {
        public const string ErrorCode = "fruitDessert.contact_missing";

        public ContactMissing(Guid entryId)
            : base($"Entry '{entryId}' has no congregation or the congregation has no dessert contact person or phone.")
        {
            Metadata.Add(ErrorMetadataKeys.StatusCode, StatusCodes.Status409Conflict);
            Metadata.Add(ErrorMetadataKeys.Title, "Contact missing");
            Metadata.Add(ErrorMetadataKeys.Code, ErrorCode);
            Metadata.Add(ErrorMetadataKeys.Params, new Dictionary<string, object> { ["entryId"] = entryId });
        }
    }

    public sealed class InvalidPhone : Error
    {
        public const string ErrorCode = "fruitDessert.invalid_phone";

        public InvalidPhone(Guid entryId)
            : base($"The contact phone of entry '{entryId}' is not a valid phone number.")
        {
            Metadata.Add(ErrorMetadataKeys.StatusCode, StatusCodes.Status409Conflict);
            Metadata.Add(ErrorMetadataKeys.Title, "Invalid phone");
            Metadata.Add(ErrorMetadataKeys.Code, ErrorCode);
            Metadata.Add(ErrorMetadataKeys.Params, new Dictionary<string, object> { ["entryId"] = entryId });
        }
    }

    public sealed class ActiveTemplateMissing : Error
    {
        public const string ErrorCode = "fruitDessert.active_template_missing";

        public ActiveTemplateMissing(string type)
            : base($"There is no active message template of type '{type}'.")
        {
            Metadata.Add(ErrorMetadataKeys.StatusCode, StatusCodes.Status409Conflict);
            Metadata.Add(ErrorMetadataKeys.Title, "Active template missing");
            Metadata.Add(ErrorMetadataKeys.Code, ErrorCode);
            Metadata.Add(ErrorMetadataKeys.Params, new Dictionary<string, object> { ["type"] = type });
        }
    }

    public sealed class UnknownPlaceholder : Error
    {
        public const string ErrorCode = "fruitDessert.unknown_placeholder";

        public UnknownPlaceholder(IReadOnlyCollection<string> placeholders)
            : base($"The message template contains unknown placeholders: {string.Join(", ", placeholders)}.")
        {
            Metadata.Add(ErrorMetadataKeys.StatusCode, StatusCodes.Status409Conflict);
            Metadata.Add(ErrorMetadataKeys.Title, "Unknown placeholder");
            Metadata.Add(ErrorMetadataKeys.Code, ErrorCode);
            Metadata.Add(ErrorMetadataKeys.Params,
                new Dictionary<string, object> { ["placeholders"] = string.Join(", ", placeholders) });
        }
    }
}

using Microsoft.AspNetCore.Http;

namespace skestock.Application.Common.Errors;

public static class EntityHistoryErrors
{
    public sealed class UnknownEntity : Error
    {
        public const string ErrorCode = "entity_history.unknown_entity";

        public UnknownEntity(string entity)
            : base($"No history is available for '{entity}'.")
        {
            Metadata.Add(ErrorMetadataKeys.StatusCode, StatusCodes.Status404NotFound);
            Metadata.Add(ErrorMetadataKeys.Title, "Unknown entity");
            Metadata.Add(ErrorMetadataKeys.Code, ErrorCode);
            Metadata.Add(ErrorMetadataKeys.Params, new Dictionary<string, object> { ["entity"] = entity });
        }
    }

    public sealed class InvalidEntityId : Error
    {
        public const string ErrorCode = "entity_history.invalid_id";

        public InvalidEntityId(string entity, string id)
            : base($"'{id}' is not a valid id for '{entity}'.")
        {
            Metadata.Add(ErrorMetadataKeys.StatusCode, StatusCodes.Status400BadRequest);
            Metadata.Add(ErrorMetadataKeys.Title, "Invalid entity id");
            Metadata.Add(ErrorMetadataKeys.Code, ErrorCode);
            Metadata.Add(ErrorMetadataKeys.Params, new Dictionary<string, object> { ["entity"] = entity, ["id"] = id });
        }
    }
}

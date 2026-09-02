namespace skestock.Application.Common.Errors;
using Microsoft.AspNetCore.Http;

public static class LocationErrors
{
    public sealed class LocationNotFound : Error
    {
        public const string ErrorCode = "locations.not_found";

        public LocationNotFound(Guid locationId) : base($"Location with id '{locationId}' was not found.")
        {
            Metadata.Add(ErrorMetadataKeys.StatusCode, StatusCodes.Status404NotFound);
            Metadata.Add(ErrorMetadataKeys.Title, "Location not found");
            Metadata.Add(ErrorMetadataKeys.Code, ErrorCode);
            Metadata.Add(ErrorMetadataKeys.Params, new Dictionary<string, object> { ["locationId"] = locationId });
        }
    }
}

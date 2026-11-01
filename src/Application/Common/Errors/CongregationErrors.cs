using Microsoft.AspNetCore.Http;

namespace skestock.Application.Common.Errors;

public static class CongregationErrors
{
    public sealed class CongregationNotFound : Error
    {
        public const string ErrorCode = "congregations.not_found";

        public CongregationNotFound(Guid congregationId)
            : base($"Congregation with id '{congregationId}' was not found.")
        {
            Metadata.Add(ErrorMetadataKeys.StatusCode, StatusCodes.Status404NotFound);
            Metadata.Add(ErrorMetadataKeys.Title, "Congregation not found");
            Metadata.Add(ErrorMetadataKeys.Code, ErrorCode);
            Metadata.Add(ErrorMetadataKeys.Params, new Dictionary<string, object> { ["congregationId"] = congregationId });
        }
    }
}

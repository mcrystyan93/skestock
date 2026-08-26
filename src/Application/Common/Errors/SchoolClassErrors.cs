namespace skestock.Application.Common.Errors;
using Microsoft.AspNetCore.Http;

public static class SchoolClassErrors
{
    public sealed class SchoolClassNotFound : Error
    {
        public const string ErrorCode = "school_classes.not_found";

        public SchoolClassNotFound(int schoolClassId) : base($"School class with id '{schoolClassId}' was not found.")
        {
            Metadata.Add(ErrorMetadataKeys.StatusCode, StatusCodes.Status404NotFound);
            Metadata.Add(ErrorMetadataKeys.Title, "School class not found");
            Metadata.Add(ErrorMetadataKeys.Code, ErrorCode);
            Metadata.Add(ErrorMetadataKeys.Params, new Dictionary<string, object> { ["schoolClassId"] = schoolClassId });
        }
    }
}

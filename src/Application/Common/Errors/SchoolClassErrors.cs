namespace skestock.Application.Common.Errors;

using Microsoft.AspNetCore.Http;

public static class SchoolClassErrors
{
    public sealed class ConfigurationRequired : Error
    {
        public ConfigurationRequired() : base("A shared class configuration must be saved before creating a class.")
        {
            Metadata.Add(ErrorMetadataKeys.StatusCode, StatusCodes.Status409Conflict);
            Metadata.Add(ErrorMetadataKeys.Title, "Class configuration required");
            Metadata.Add(ErrorMetadataKeys.Code, "school_classes.configuration_required");
        }
    }

    public sealed class ConfigurationAlreadyInitialized : Error
    {
        public ConfigurationAlreadyInitialized(Guid schoolClassId)
            : base($"School class '{schoolClassId}' already has its initial configuration.")
        {
            Metadata.Add(ErrorMetadataKeys.StatusCode, StatusCodes.Status409Conflict);
            Metadata.Add(ErrorMetadataKeys.Title, "Class configuration already initialized");
            Metadata.Add(ErrorMetadataKeys.Code, "school_classes.configuration_already_initialized");
        }
    }

    public sealed class ClassDepartmentNotFound : Error
    {
        public ClassDepartmentNotFound(Guid schoolClassId, Guid departmentId)
            : base($"Department '{departmentId}' was not found in school class '{schoolClassId}'.")
        {
            Metadata.Add(ErrorMetadataKeys.StatusCode, StatusCodes.Status404NotFound);
            Metadata.Add(ErrorMetadataKeys.Title, "Class department not found");
            Metadata.Add(ErrorMetadataKeys.Code, "school_classes.department_not_found");
        }
    }

    public sealed class SchoolClassNotFound : Error
    {
        public const string ErrorCode = "school_classes.not_found";

        public SchoolClassNotFound(Guid schoolClassId) : base($"School class with id '{schoolClassId}' was not found.")
        {
            Metadata.Add(ErrorMetadataKeys.StatusCode, StatusCodes.Status404NotFound);
            Metadata.Add(ErrorMetadataKeys.Title, "School class not found");
            Metadata.Add(ErrorMetadataKeys.Code, ErrorCode);
            Metadata.Add(ErrorMetadataKeys.Params,
                new Dictionary<string, object> { ["schoolClassId"] = schoolClassId });
        }
    }
}

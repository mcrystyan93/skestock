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

    public sealed class StatusChangeNotAllowed : Error
    {
        public StatusChangeNotAllowed(Guid schoolClassId)
            : base($"School class '{schoolClassId}' cannot be closed or reopened by editing its status; use the close action.")
        {
            Metadata.Add(ErrorMetadataKeys.StatusCode, StatusCodes.Status409Conflict);
            Metadata.Add(ErrorMetadataKeys.Title, "Status change not allowed");
            Metadata.Add(ErrorMetadataKeys.Code, "school_classes.status_change_not_allowed");
        }
    }

    public sealed class SchoolClassClosed : Error
    {
        public SchoolClassClosed(Guid schoolClassId)
            : base($"School class '{schoolClassId}' is closed and can no longer be changed.")
        {
            Metadata.Add(ErrorMetadataKeys.StatusCode, StatusCodes.Status409Conflict);
            Metadata.Add(ErrorMetadataKeys.Title, "School class is closed");
            Metadata.Add(ErrorMetadataKeys.Code, "school_classes.closed");
        }
    }

    public sealed class AlreadyClosed : Error
    {
        public AlreadyClosed(Guid schoolClassId) : base($"School class '{schoolClassId}' is already closed.")
        {
            Metadata.Add(ErrorMetadataKeys.StatusCode, StatusCodes.Status409Conflict);
            Metadata.Add(ErrorMetadataKeys.Title, "School class already closed");
            Metadata.Add(ErrorMetadataKeys.Code, "school_classes.already_closed");
        }
    }

    public sealed class ConcurrentChange : Error
    {
        public ConcurrentChange(Guid schoolClassId)
            : base($"School class '{schoolClassId}' or its stock was changed by another operation. Please reload and try again.")
        {
            Metadata.Add(ErrorMetadataKeys.StatusCode, StatusCodes.Status409Conflict);
            Metadata.Add(ErrorMetadataKeys.Title, "School class changed concurrently");
            Metadata.Add(ErrorMetadataKeys.Code, "school_classes.concurrent_change");
        }
    }

    public sealed class TargetNotOpen : Error
    {
        public TargetNotOpen(Guid schoolClassId)
            : base($"School class '{schoolClassId}' is closed and cannot receive stock.")
        {
            Metadata.Add(ErrorMetadataKeys.StatusCode, StatusCodes.Status409Conflict);
            Metadata.Add(ErrorMetadataKeys.Title, "Target school class is closed");
            Metadata.Add(ErrorMetadataKeys.Code, "school_classes.target_not_open");
        }
    }
}

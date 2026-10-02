using Microsoft.AspNetCore.Http;

namespace skestock.Application.Common.Errors;

public static class ClassConfigurationErrors
{
    public sealed class DepartmentNotFound : Error
    {
        public DepartmentNotFound(Guid departmentId) : base($"Department '{departmentId}' was not found in the shared configuration.")
        {
            Metadata.Add(ErrorMetadataKeys.StatusCode, StatusCodes.Status404NotFound);
            Metadata.Add(ErrorMetadataKeys.Title, "Department not found");
            Metadata.Add(ErrorMetadataKeys.Code, "class_configuration.department_not_found");
            Metadata.Add(ErrorMetadataKeys.Params, new Dictionary<string, object> { ["departmentId"] = departmentId });
        }
    }
}

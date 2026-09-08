namespace skestock.Application.Common.Errors;
using Microsoft.AspNetCore.Http;
using skestock.Domain.Enums;

public static class CategoryErrors
{
    public sealed class CategoryNotFound : Error
    {
        public const string ErrorCode = "categories.not_found";

        public CategoryNotFound(Guid categoryId) : base($"Category with id '{categoryId}' was not found.")
        {
            Metadata.Add(ErrorMetadataKeys.StatusCode, StatusCodes.Status404NotFound);
            Metadata.Add(ErrorMetadataKeys.Title, "Category not found");
            Metadata.Add(ErrorMetadataKeys.Code, ErrorCode);
            Metadata.Add(ErrorMetadataKeys.Params, new Dictionary<string, object> { ["categoryId"] = categoryId });
        }
    }
}

public static class CategoryImportErrors
{
    public sealed class FileMetadataNotConfirmed : Error
    {
        public const string ErrorCode = "category_imports.file_not_confirmed";

        public FileMetadataNotConfirmed(Guid fileMetadataId)
            : base($"File metadata with id '{fileMetadataId}' was not found or is not confirmed.")
        {
            Metadata.Add(ErrorMetadataKeys.StatusCode, StatusCodes.Status409Conflict);
            Metadata.Add(ErrorMetadataKeys.Title, "File upload not confirmed");
            Metadata.Add(ErrorMetadataKeys.Code, ErrorCode);
            Metadata.Add(ErrorMetadataKeys.Params, new Dictionary<string, object>
            {
                ["fileMetadataId"] = fileMetadataId
            });
        }
    }

    public sealed class CategoryImportNotFound : Error
    {
        public const string ErrorCode = "category_imports.not_found";

        public CategoryImportNotFound(Guid categoryImportId) : base($"Category import with id '{categoryImportId}' was not found.")
        {
            Metadata.Add(ErrorMetadataKeys.StatusCode, StatusCodes.Status404NotFound);
            Metadata.Add(ErrorMetadataKeys.Title, "Category import not found");
            Metadata.Add(ErrorMetadataKeys.Code, ErrorCode);
            Metadata.Add(ErrorMetadataKeys.Params, new Dictionary<string, object> { ["categoryImportId"] = categoryImportId });
        }
    }

    public sealed class CategoryImportNotInReview : Error
    {
        public const string ErrorCode = "category_imports.not_in_review";

        public CategoryImportNotInReview(Guid categoryImportId, CategoryImportStatus status)
            : base($"Category import with id '{categoryImportId}' is '{status}' and cannot be confirmed. Only imports pending review can be confirmed.")
        {
            Metadata.Add(ErrorMetadataKeys.StatusCode, StatusCodes.Status409Conflict);
            Metadata.Add(ErrorMetadataKeys.Title, "Category import not pending review");
            Metadata.Add(ErrorMetadataKeys.Code, ErrorCode);
            Metadata.Add(ErrorMetadataKeys.Params, new Dictionary<string, object>
            {
                ["categoryImportId"] = categoryImportId,
                ["status"] = status.ToString()
            });
        }
    }
}

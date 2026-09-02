namespace skestock.Application.Common.Errors;
using Microsoft.AspNetCore.Http;

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

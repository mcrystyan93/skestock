namespace skestock.Application.Common.Errors;
using Microsoft.AspNetCore.Http;

public static class StorageErrors
{
    public sealed class FileNotFound : Error
    {
        public const string ErrorCode = "storage.file_not_found";

        public FileNotFound(Guid fileId) : base($"File with id '{fileId}' was not found.")
        {
            Metadata.Add(ErrorMetadataKeys.StatusCode, StatusCodes.Status404NotFound);
            Metadata.Add(ErrorMetadataKeys.Title, "File not found");
            Metadata.Add(ErrorMetadataKeys.Code, ErrorCode);
            Metadata.Add(ErrorMetadataKeys.Params, new Dictionary<string, object> { ["fileId"] = fileId });
        }
    }

    public sealed class BlobNotFound : Error
    {
        public const string ErrorCode = "storage.blob_not_found";

        public BlobNotFound(Guid fileId) : base(
            $"The blob for file with id '{fileId}' was not found in storage; the upload did not complete.")
        {
            Metadata.Add(ErrorMetadataKeys.StatusCode, StatusCodes.Status409Conflict);
            Metadata.Add(ErrorMetadataKeys.Title, "Upload not completed");
            Metadata.Add(ErrorMetadataKeys.Code, ErrorCode);
            Metadata.Add(ErrorMetadataKeys.Params, new Dictionary<string, object> { ["fileId"] = fileId });
        }
    }
}

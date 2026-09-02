namespace skestock.Application.Storage.Models;

public static class StorageRequests
{
    public class RequestUploadRequest
    {
        public string FileName { get; init; } = string.Empty;
        public string ContentType { get; init; } = string.Empty;
    }

    public class ConfirmUploadRequest
    {
        public Guid FileId { get; init; }
    }
}

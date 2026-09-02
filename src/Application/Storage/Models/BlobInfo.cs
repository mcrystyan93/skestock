namespace skestock.Application.Storage.Models;

public record BlobInfo(long SizeBytes, string ETag, string ContentType);

namespace skestock.Application.Storage.DTOs;

public record UploadDto(string ContainerName, string BlobPath, Stream Content, string ContentType);

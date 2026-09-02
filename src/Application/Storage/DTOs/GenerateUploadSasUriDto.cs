namespace skestock.Application.Storage.DTOs;

public record GenerateUploadSasUriDto(string ContainerName, string BlobPath, TimeSpan ValidFor);

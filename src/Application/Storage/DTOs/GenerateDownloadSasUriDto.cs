namespace skestock.Application.Storage.DTOs;

public record GenerateDownloadSasUriDto(string ContainerName, string BlobPath, TimeSpan ValidFor);

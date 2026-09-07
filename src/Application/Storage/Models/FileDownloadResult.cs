using skestock.Application.Storage.DTOs;

namespace skestock.Application.Storage.Models;

public record FileDownloadResult(FileMetadataDto File, Uri DownloadUrl);

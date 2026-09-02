using skestock.Domain.Entities;
using skestock.Domain.Enums;

namespace skestock.Application.Storage.DTOs;

public record FileMetadataDto(
    Guid Id,
    Guid FileId,
    string OriginalName,
    string BlobContainer,
    string BlobPath,
    string ContentType,
    long SizeBytes,
    FileStatus Status,
    string? ETag,
    DateTimeOffset? CompletedDate)
{
    public static FileMetadataDto FromEntity(FileMetadata file) => new(
        file.Id,
        file.FileId,
        file.OriginalName,
        file.BlobContainer,
        file.BlobPath,
        file.ContentType,
        file.SizeBytes,
        file.Status,
        file.ETag,
        file.CompletedDate);
}

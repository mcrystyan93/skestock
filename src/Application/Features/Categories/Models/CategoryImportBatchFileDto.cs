using skestock.Domain.Enums;

namespace skestock.Application.Features.Categories.Models;

public record CategoryImportBatchFileDto
{
    public Guid FileMetadataId { get; init; }
    public string OriginalName { get; init; } = string.Empty;
    public string BlobPath { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public long SizeBytes { get; init; }
    public FileStatus Status { get; init; }
    public int SortOrder { get; init; }
}

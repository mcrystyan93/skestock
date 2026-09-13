using skestock.Domain.Enums;

namespace skestock.Application.Features.Items.Models;

/// <summary>One file included in an item import batch, in upload order.</summary>
public record ItemImportBatchFileDto
{
    public Guid FileMetadataId { get; init; }
    public string OriginalName { get; init; } = string.Empty;
    public string BlobPath { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public long SizeBytes { get; init; }
    public FileStatus Status { get; init; }
    public int SortOrder { get; init; }
}

using skestock.Domain.Enums;

namespace skestock.Domain.Entities;

public class FileMetadata : BaseAuditableEntity
{
    public Guid FileId { get; set; }
    public string OriginalName { get; set; } = null!;

    public string BlobContainer { get; set; } = null!;

    public string BlobPath { get; set; } = null!;

    public string ContentType { get; set; } = null!;

    public long SizeBytes { get; set; }

    public FileStatus Status { get; set; }

    public string? ETag { get; set; }

    public DateTimeOffset? CompletedDate { get; set; }
}

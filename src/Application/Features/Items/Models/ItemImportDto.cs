using skestock.Domain.Enums;

namespace skestock.Application.Features.Items.Models;

public record ItemImportDto
{
    public Guid Id { get; init; }
    public ItemImportStatus Status { get; init; }
    public Guid FileMetadataId { get; init; }
    public string BlobPath { get; init; } = string.Empty;
    public DateTime UploadedAt { get; init; }
    public DateTime? ProcessedAt { get; init; }
    public string? ErrorMessage { get; init; }
}

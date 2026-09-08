using skestock.Domain.Enums;

namespace skestock.Application.Features.Categories.Models;

public record CategoryImportDto
{
    public Guid Id { get; init; }
    public CategoryImportStatus Status { get; init; }
    public Guid FileMetadataId { get; init; }
    public string BlobPath { get; init; } = string.Empty;
    public DateTime UploadedAt { get; init; }
    public DateTime? ProcessedAt { get; init; }
    public string? ErrorMessage { get; init; }
}

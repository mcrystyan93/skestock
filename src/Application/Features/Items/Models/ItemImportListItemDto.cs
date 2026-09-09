using skestock.Domain.Enums;

namespace skestock.Application.Features.Items.Models;

/// <summary>
/// Row shape for the paginated item-import list. Flattens the uploader name in so callers don't have
/// to make follow-up requests to resolve it. Mirrors <see cref="Categories.Models.CategoryImportListItemDto"/>.
/// </summary>
public record ItemImportListItemDto
{
    public Guid Id { get; init; }
    public ItemImportStatus Status { get; init; }
    public Guid FileMetadataId { get; init; }
    public string BlobPath { get; init; } = string.Empty;
    public string? ErrorMessage { get; init; }
    public string? UploadedByName { get; init; }
    public DateTime UploadedAt { get; init; }
    public DateTime? ProcessedAt { get; init; }
    public DateTimeOffset CreatedDate { get; init; }
}

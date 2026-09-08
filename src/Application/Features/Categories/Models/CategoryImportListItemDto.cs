using skestock.Domain.Enums;

namespace skestock.Application.Features.Categories.Models;

/// <summary>
/// Row shape for the paginated category-import list. Flattens the uploader name in so callers don't
/// have to make follow-up requests to resolve it. Mirrors <see cref="GoodsReceipts.Models.GoodsReceiptImportListItemDto"/>.
/// </summary>
public record CategoryImportListItemDto
{
    public Guid Id { get; init; }
    public CategoryImportStatus Status { get; init; }
    public Guid FileMetadataId { get; init; }
    public string BlobPath { get; init; } = string.Empty;
    public string? ErrorMessage { get; init; }
    public string? UploadedByName { get; init; }
    public DateTime UploadedAt { get; init; }
    public DateTime? ProcessedAt { get; init; }
    public DateTimeOffset CreatedDate { get; init; }
}

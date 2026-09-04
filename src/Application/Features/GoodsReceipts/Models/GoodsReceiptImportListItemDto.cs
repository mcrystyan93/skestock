using skestock.Domain.Enums;

namespace skestock.Application.Features.GoodsReceipts.Models;

/// <summary>
/// Row shape for the paginated goods-receipt-import list. Flattens the class name in so callers
/// don't have to make follow-up requests to resolve it.
/// </summary>
public record GoodsReceiptImportListItemDto
{
    public Guid Id { get; init; }
    public GoodsReceiptImportStatus Status { get; init; }
    public Guid ClassId { get; init; }
    public string ClassName { get; init; } = string.Empty;
    public Guid FileMetadataId { get; init; }
    public string BlobPath { get; init; } = string.Empty;
    public string? ErrorMessage { get; init; }
    public string? UploadedByName { get; init; }
    public DateTime UploadedAt { get; init; }
    public DateTime? ProcessedAt { get; init; }
    public DateTimeOffset CreatedDate { get; init; }
}

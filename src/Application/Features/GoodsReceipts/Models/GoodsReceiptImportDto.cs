using skestock.Domain.Enums;

namespace skestock.Application.Features.GoodsReceipts.Models;

public record GoodsReceiptImportDto
{
    public Guid Id { get; init; }
    public GoodsReceiptImportStatus Status { get; init; }
    public Guid ClassId { get; init; }
    public Guid FileMetadataId { get; init; }
    public string BlobPath { get; init; } = string.Empty;
    public DateTime UploadedAt { get; init; }
}

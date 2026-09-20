using skestock.Domain.Enums;
using skestock.Domain.Events.GoodsReceipt;

namespace skestock.Domain.Entities;

public class GoodsReceiptImport : BaseAuditableEntity, IKeysetEntity
{
    public string BlobPath { get; set; } = null!;
    public GoodsReceiptImportStatus Status { get; set; } = GoodsReceiptImportStatus.Processing;
    public string? ErrorMessage { get; set; } // populated when Status == Failed

    public Guid ClassId { get; set; } // locked in at upload time, not at confirm time
    public SchoolClass Class { get; set; } = null!;

    public Guid FileMetadataId { get; set; }
    public FileMetadata FileMetadata { get; set; } = null!;

    public Guid UploadedByUserId { get; set; }
    public UserProfile UploadedByUser { get; set; } = null!;

    public DateTimeOffset UploadedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ProcessedAt { get; set; }

    // Set only once the user confirms and a real GoodsReceipt is created from this import
    public Guid? ResultingGoodsReceiptId { get; set; }
    public GoodsReceipt? ResultingGoodsReceipt { get; set; }
    
    public string? ExtractedDataJson { get; private set; }

    public ICollection<GoodsReceiptImportLine> Lines { get; set; } = new List<GoodsReceiptImportLine>();

    public static GoodsReceiptImport Create(Guid classId, Guid fileMetadataId, Guid uploadedByUserId, string blobPath)
    {
        var import = new GoodsReceiptImport
        {
            Id = Guid.CreateVersion7(),
            ClassId = classId,
            Class = null!,
            FileMetadataId = fileMetadataId,
            FileMetadata = null!,
            UploadedByUserId = uploadedByUserId,
            UploadedByUser = null!,
            BlobPath = blobPath,
            Status = GoodsReceiptImportStatus.Processing
        };

        import.AddDomainEvent(new GoodsReceiptImportCreatedEvent(import));
        
        return import;
    }

    public void ApplyExtractionResult(string extractedDataJson)
    {
        ExtractedDataJson = extractedDataJson;
        Status = GoodsReceiptImportStatus.PendingReview;
        ProcessedAt = DateTimeOffset.UtcNow;
        
        AddDomainEvent(new GoodsReceiptImportCompletedEvent(Id, UploadedByUserId));
    }

    public void MarkAsFailed(string errorMessage)
    {
        Status = GoodsReceiptImportStatus.Failed;
        ErrorMessage = errorMessage;
        
        AddDomainEvent(new GoodsReceiptImportFailedEvent(Id));
    }

    // Called once the user has reviewed the extracted lines and a real GoodsReceipt has been
    // created from this import. Links the import to that receipt and closes it out.
    public void MarkAsConfirmed(Guid goodsReceiptId)
    {
        Status = GoodsReceiptImportStatus.Confirmed;
        ResultingGoodsReceiptId = goodsReceiptId;
        ProcessedAt ??= DateTimeOffset.UtcNow;
        
        AddDomainEvent(new GoodsReceiptImportConfirmedEvent(Id));
    }
}

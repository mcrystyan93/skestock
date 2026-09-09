using skestock.Domain.Enums;
using skestock.Domain.Events.Items;

namespace skestock.Domain.Entities;

public class ItemImport : BaseAuditableEntity, IKeysetEntity
{
    public string BlobPath { get; set; } = null!;
    public ItemImportStatus Status { get; set; } = ItemImportStatus.Processing;
    public string? ErrorMessage { get; set; } // populated when Status == Failed

    public Guid FileMetadataId { get; set; }
    public FileMetadata FileMetadata { get; set; } = null!;

    public Guid UploadedByUserId { get; set; }
    public UserProfile UploadedByUser { get; set; } = null!;

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }

    // Serialized item suggestions produced by the AI extraction, awaiting user review.
    public string? ExtractedDataJson { get; private set; }

    // Serialized confirmation result (the items/categories that resulted from the reviewed list).
    // Persisted so a repeated confirmation of an already-Confirmed import can return the same result
    // idempotently instead of creating items again.
    public string? ConfirmationResultJson { get; private set; }

    public Guid ConcurrencyStamp { get; private set; } = Guid.NewGuid();

    public static ItemImport Create(Guid fileMetadataId, Guid uploadedByUserId, string blobPath)
    {
        var import = new ItemImport
        {
            Id = Guid.CreateVersion7(),
            FileMetadataId = fileMetadataId,
            FileMetadata = null!,
            UploadedByUserId = uploadedByUserId,
            UploadedByUser = null!,
            BlobPath = blobPath,
            Status = ItemImportStatus.Processing
        };

        import.AddDomainEvent(new ItemImportCreatedEvent(import));

        return import;
    }

    public void ApplyExtractionResult(string extractedDataJson)
    {
        ExtractedDataJson = extractedDataJson;
        Status = ItemImportStatus.PendingReview;
        ProcessedAt = DateTime.UtcNow;
        ConcurrencyStamp = Guid.NewGuid();

        AddDomainEvent(new ItemImportCompletedEvent(Id, UploadedByUserId));
    }

    public void MarkAsFailed(string errorMessage)
    {
        Status = ItemImportStatus.Failed;
        ErrorMessage = errorMessage;
        ConcurrencyStamp = Guid.NewGuid();

        AddDomainEvent(new ItemImportFailedEvent(Id));
    }

    // Called once the user has reviewed the suggested items and the reviewed items (and any missing
    // categories) have been created. Records the result payload and closes the import out.
    public void MarkAsConfirmed(string confirmationResultJson)
    {
        Status = ItemImportStatus.Confirmed;
        ConfirmationResultJson = confirmationResultJson;
        ProcessedAt ??= DateTime.UtcNow;
        ConcurrencyStamp = Guid.NewGuid();

        AddDomainEvent(new ItemImportConfirmedEvent(Id));
    }
}

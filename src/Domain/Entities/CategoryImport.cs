using skestock.Domain.Enums;
using skestock.Domain.Events.Categories;

namespace skestock.Domain.Entities;

public class CategoryImport : BaseAuditableEntity, IKeysetEntity
{
    public string BlobPath { get; set; } = null!;
    public CategoryImportStatus Status { get; set; } = CategoryImportStatus.Processing;
    public string? ErrorMessage { get; set; } // populated when Status == Failed

    public Guid FileMetadataId { get; set; }
    public FileMetadata FileMetadata { get; set; } = null!;

    public Guid UploadedByUserId { get; set; }
    public UserProfile UploadedByUser { get; set; } = null!;

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }

    // Serialized category-name suggestions produced by the AI extraction, awaiting user review.
    public string? ExtractedDataJson { get; private set; }

    // Serialized confirmation result (the categories that resulted from the reviewed list). Persisted
    // so a repeated confirmation of an already-Confirmed import can return the same result idempotently
    // instead of creating categories again.
    public string? ConfirmationResultJson { get; private set; }

    public static CategoryImport Create(Guid fileMetadataId, Guid uploadedByUserId, string blobPath)
    {
        var import = new CategoryImport
        {
            Id = Guid.CreateVersion7(),
            FileMetadataId = fileMetadataId,
            FileMetadata = null!,
            UploadedByUserId = uploadedByUserId,
            UploadedByUser = null!,
            BlobPath = blobPath,
            Status = CategoryImportStatus.Processing
        };

        import.AddDomainEvent(new CategoryImportCreatedEvent(import));

        return import;
    }

    public void ApplyExtractionResult(string extractedDataJson)
    {
        ExtractedDataJson = extractedDataJson;
        Status = CategoryImportStatus.PendingReview;
        ProcessedAt = DateTime.UtcNow;

        AddDomainEvent(new CategoryImportCompletedEvent(Id, UploadedByUserId));
    }

    public void MarkAsFailed(string errorMessage)
    {
        Status = CategoryImportStatus.Failed;
        ErrorMessage = errorMessage;

        AddDomainEvent(new CategoryImportFailedEvent(Id));
    }

    // Called once the user has reviewed the suggested category names and the reviewed categories have
    // been created. Records the result payload and closes the import out.
    public void MarkAsConfirmed(string confirmationResultJson)
    {
        Status = CategoryImportStatus.Confirmed;
        ConfirmationResultJson = confirmationResultJson;
        ProcessedAt ??= DateTime.UtcNow;

        AddDomainEvent(new CategoryImportConfirmedEvent(Id));
    }
}

using skestock.Domain.Enums;
using skestock.Domain.Events.Categories;

namespace skestock.Domain.Entities;

/// <summary>
/// An aggregate containing one or more uploaded files whose category suggestions are extracted
/// together and reviewed as one operation.
/// </summary>
public class CategoryImportBatch : BaseAuditableEntity, IKeysetEntity
{
    public CategoryImportBatchStatus Status { get; set; } = CategoryImportBatchStatus.Processing;
    public string? ErrorMessage { get; set; }

    public Guid UploadedByUserId { get; set; }
    public UserProfile UploadedByUser { get; set; } = null!;

    public Guid? ClientRequestId { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    public int AttemptCount { get; private set; }

    public string? ExtractedDataJson { get; private set; }
    public string? ConfirmationResultJson { get; private set; }
    public Guid ConcurrencyStamp { get; private set; } = Guid.NewGuid();
    public DateTime? ProcessingLeaseUntilUtc { get; private set; }

    public ICollection<CategoryImportBatchFile> Files { get; set; } = new List<CategoryImportBatchFile>();
    public ICollection<ImportBatchHistory> History { get; set; } = new List<ImportBatchHistory>();

    public static CategoryImportBatch Create(
        Guid uploadedByUserId, Guid? clientRequestId, IReadOnlyCollection<Guid> fileMetadataIds)
    {
        if (fileMetadataIds is null || fileMetadataIds.Count < 1)
            throw new ArgumentException("A category import batch requires at least one file.", nameof(fileMetadataIds));

        var batch = new CategoryImportBatch
        {
            Id = Guid.CreateVersion7(),
            UploadedByUserId = uploadedByUserId,
            UploadedByUser = null!,
            ClientRequestId = clientRequestId
        };
        batch.History.Add(ImportBatchHistory.Created());

        var order = 0;
        foreach (var fileMetadataId in fileMetadataIds)
        {
            batch.Files.Add(new CategoryImportBatchFile
            {
                Id = Guid.CreateVersion7(),
                CategoryImportBatchId = batch.Id,
                Batch = batch,
                FileMetadataId = fileMetadataId,
                SortOrder = order++
            });
        }

        batch.AddDomainEvent(new CategoryImportBatchCreatedEvent(batch));
        return batch;
    }

    public void RecordAttempt(DateTime processingLeaseUntilUtc)
    {
        AttemptCount++;
        ProcessingLeaseUntilUtc = processingLeaseUntilUtc;
        ConcurrencyStamp = Guid.NewGuid();
    }

    public bool HasActiveProcessingLease(DateTime utcNow) =>
        Status == CategoryImportBatchStatus.Processing &&
        ProcessingLeaseUntilUtc is { } leaseUntilUtc &&
        leaseUntilUtc > utcNow;

    public void ReleaseProcessingLease()
    {
        Status = CategoryImportBatchStatus.Processing;
        ProcessingLeaseUntilUtc = null;
        ConcurrencyStamp = Guid.NewGuid();
    }

    public void ApplyExtractionResult(string extractedDataJson)
    {
        ExtractedDataJson = extractedDataJson;
        Status = CategoryImportBatchStatus.PendingReview;
        ProcessedAt = DateTime.UtcNow;
        ProcessingLeaseUntilUtc = null;
        ConcurrencyStamp = Guid.NewGuid();
        AddDomainEvent(new CategoryImportBatchCompletedEvent(Id, UploadedByUserId));
    }

    public void MarkAsFailed(string errorMessage)
    {
        Status = CategoryImportBatchStatus.Failed;
        ErrorMessage = errorMessage;
        ProcessingLeaseUntilUtc = null;
        ConcurrencyStamp = Guid.NewGuid();
        AddDomainEvent(new CategoryImportBatchFailedEvent(Id));
    }

    public void MarkAsConfirmed(string confirmationResultJson)
    {
        Status = CategoryImportBatchStatus.Confirmed;
        ConfirmationResultJson = confirmationResultJson;
        ProcessedAt ??= DateTime.UtcNow;
        ProcessingLeaseUntilUtc = null;
        ConcurrencyStamp = Guid.NewGuid();
        AddDomainEvent(new CategoryImportBatchConfirmedEvent(Id));
    }
}

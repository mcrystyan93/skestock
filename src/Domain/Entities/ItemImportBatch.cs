using skestock.Domain.Enums;
using skestock.Domain.Events.Items;

namespace skestock.Domain.Entities;

/// <summary>
/// An aggregate (multi-file) item import: one or more previously-uploaded files are extracted
/// together in a single AI request and their suggested items are merged into one reviewable result.
/// Carries a collection of <see cref="ItemImportBatchFile"/> rows instead of a single blob path.
/// </summary>
public class ItemImportBatch : BaseAuditableEntity, IKeysetEntity
{
    public ItemImportBatchStatus Status { get; set; } = ItemImportBatchStatus.Processing;
    public string? ErrorMessage { get; set; } // populated when Status == Failed

    public Guid UploadedByUserId { get; set; }
    public UserProfile UploadedByUser { get; set; } = null!;

    /// <summary>
    /// Optional client-supplied idempotency key. A repeated create request for the same uploader with
    /// the same key returns the existing batch instead of creating a duplicate.
    /// </summary>
    public Guid? ClientRequestId { get; set; }

    public DateTimeOffset UploadedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ProcessedAt { get; set; }

    /// <summary>Number of times processing has been attempted; incremented on every Process run.</summary>
    public int AttemptCount { get; private set; }

    public string? ExtractedDataJson { get; private set; }
    public string? ConfirmationResultJson { get; private set; }

    public Guid ConcurrencyStamp { get; private set; } = Guid.NewGuid();

    public ICollection<ItemImportBatchFile> Files { get; set; } = new List<ItemImportBatchFile>();
    public ICollection<ImportBatchHistory> History { get; set; } = new List<ImportBatchHistory>();

    public static ItemImportBatch Create(
        Guid uploadedByUserId, Guid? clientRequestId, IReadOnlyCollection<Guid> fileMetadataIds)
    {
        if (fileMetadataIds is null || fileMetadataIds.Count < 1)
            throw new ArgumentException("An item import batch requires at least one file.", nameof(fileMetadataIds));

        var batch = new ItemImportBatch
        {
            Id = Guid.CreateVersion7(),
            UploadedByUserId = uploadedByUserId,
            UploadedByUser = null!,
            ClientRequestId = clientRequestId,
            Status = ItemImportBatchStatus.Processing
        };
        batch.History.Add(ImportBatchHistory.Created());

        var order = 0;
        foreach (var fileMetadataId in fileMetadataIds)
        {
            batch.Files.Add(new ItemImportBatchFile
            {
                Id = Guid.CreateVersion7(),
                ItemImportBatchId = batch.Id,
                Batch = batch,
                FileMetadataId = fileMetadataId,
                SortOrder = order++
            });
        }

        batch.AddDomainEvent(new ItemImportBatchCreatedEvent(batch));

        return batch;
    }

    public void RecordAttempt(DateTimeOffset processingLeaseUntilUtc)
    {
        AttemptCount++;
        ProcessingLeaseUntilUtc = processingLeaseUntilUtc;
        ConcurrencyStamp = Guid.NewGuid();
    }

    public DateTimeOffset? ProcessingLeaseUntilUtc { get; private set; }

    public bool HasActiveProcessingLease(DateTimeOffset utcNow) =>
        Status == ItemImportBatchStatus.Processing &&
        ProcessingLeaseUntilUtc is { } leaseUntilUtc &&
        leaseUntilUtc > utcNow;

    public void ReleaseProcessingLease()
    {
        Status = ItemImportBatchStatus.Processing;
        ProcessingLeaseUntilUtc = null;
        ConcurrencyStamp = Guid.NewGuid();
    }

    public void ApplyExtractionResult(string extractedDataJson)
    {
        ExtractedDataJson = extractedDataJson;
        Status = ItemImportBatchStatus.PendingReview;
        ProcessedAt = DateTimeOffset.UtcNow;
        ProcessingLeaseUntilUtc = null;
        ConcurrencyStamp = Guid.NewGuid();

        AddDomainEvent(new ItemImportBatchCompletedEvent(Id, UploadedByUserId));
    }

    public void MarkAsFailed(string errorMessage)
    {
        Status = ItemImportBatchStatus.Failed;
        ErrorMessage = errorMessage;
        ProcessingLeaseUntilUtc = null;
        ConcurrencyStamp = Guid.NewGuid();

        AddDomainEvent(new ItemImportBatchFailedEvent(Id));
    }

    public void MarkAsConfirmed(string confirmationResultJson)
    {
        Status = ItemImportBatchStatus.Confirmed;
        ConfirmationResultJson = confirmationResultJson;
        ProcessedAt ??= DateTimeOffset.UtcNow;
        ProcessingLeaseUntilUtc = null;
        ConcurrencyStamp = Guid.NewGuid();

        AddDomainEvent(new ItemImportBatchConfirmedEvent(Id));
    }
}

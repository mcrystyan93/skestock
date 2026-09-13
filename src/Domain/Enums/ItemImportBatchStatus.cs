namespace skestock.Domain.Enums;

public enum ItemImportBatchStatus
{
    Processing,    // worker picked it up, calling the AI API with every file in the batch
    PendingReview, // extraction done, waiting on the user to confirm the reviewed items
    Confirmed,     // user confirmed, items (and any missing categories) were created
    Failed         // extraction failed - a file was unreadable, AI error, etc.
}

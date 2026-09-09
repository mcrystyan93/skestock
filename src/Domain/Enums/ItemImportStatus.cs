namespace skestock.Domain.Enums;

public enum ItemImportStatus
{
    Processing,    // worker picked it up, calling the AI API
    PendingReview, // extraction done, waiting on the user to confirm the reviewed items
    Confirmed,     // user confirmed, items (and any missing categories) were created
    Failed         // extraction failed - file unreadable, AI error, etc.
}

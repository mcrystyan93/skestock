namespace skestock.Domain.Enums;

public enum CategoryImportStatus
{
    Processing,    // worker picked it up, calling the AI API
    PendingReview, // extraction done, waiting on the user to confirm the reviewed names
    Confirmed,     // user confirmed, categories were created
    Failed         // extraction failed - file unreadable, AI error, etc.
}

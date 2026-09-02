namespace skestock.Domain.Enums;

public enum GoodsReceiptImportStatus
{
    Processing,   // worker picked it up, calling the AI API
    PendingReview, // extraction done, waiting on the user to confirm
    Confirmed,     // user confirmed, GoodsReceipt was created
    Failed         // extraction failed - file unreadable, AI error, etc.

}

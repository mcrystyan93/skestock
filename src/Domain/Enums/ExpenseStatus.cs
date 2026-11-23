namespace skestock.Domain.Enums;

// Persisted as int: only append, never reorder.
public enum ExpenseStatus
{
    Processing, // queued for / being read by the AI
    Processed,  // data available (extracted, manual, or completed by hand after a failure)
    Failed      // extraction failed; the user can retry or fill the data in manually
}

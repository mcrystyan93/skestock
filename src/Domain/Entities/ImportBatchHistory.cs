using skestock.Domain.Enums;

namespace skestock.Domain.Entities;

/// <summary>
/// Immutable lifecycle/attempt entry for an aggregate import.
/// </summary>
public sealed class ImportBatchHistory
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public ImportBatchHistoryStatus Status { get; set; }
    public int Attempt { get; set; }
    public string? Message { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public static ImportBatchHistory Created() => new()
    {
        Status = ImportBatchHistoryStatus.Created,
        Attempt = 0
    };

    public static ImportBatchHistory Processing(int attempt) => new()
    {
        Status = ImportBatchHistoryStatus.Processing,
        Attempt = attempt
    };

    public static ImportBatchHistory Completed(int attempt) => new()
    {
        Status = ImportBatchHistoryStatus.Completed,
        Attempt = attempt
    };

    public static ImportBatchHistory Failed(int attempt, string message) => new()
    {
        Status = ImportBatchHistoryStatus.Failed,
        Attempt = attempt,
        Message = message
    };

    public static ImportBatchHistory Confirmed() => new()
    {
        Status = ImportBatchHistoryStatus.Confirmed
    };
}

using skestock.Domain.Queues;

namespace skestock.Application.Queues.Interfaces;

/// <summary>
/// Owns the database-side lifecycle of an outbox claim. The publisher only orchestrates queue
/// sends; claim ownership, lease recovery, and conditional state transitions stay behind this seam.
/// </summary>
public interface IOutboxClaimStore
{
    Task<OutboxClaimBatch> ClaimBatchAsync(
        OutboxClaimOptions options,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken);

    Task<bool> MarkPublishedAsync(
        OutboxClaimBatch claim,
        Guid messageId,
        DateTimeOffset processedAtUtc,
        CancellationToken cancellationToken);

    Task<int?> RecordFailureAsync(
        OutboxClaimBatch claim,
        Guid messageId,
        string error,
        CancellationToken cancellationToken);
}

/// <summary>
/// Policy values for one outbox claim attempt. The store applies these values atomically when it
/// selects and leases pending rows.
/// </summary>
public sealed record OutboxClaimOptions(int BatchSize, int MaxRetries, TimeSpan Lease);

/// <summary>
/// The publisher's ownership token and the immutable message snapshot loaded while the claim was
/// committed. Completion and failure updates must use this same token.
/// </summary>
public sealed record OutboxClaimBatch(Guid ClaimId, IReadOnlyList<OutboxMessage> Messages);

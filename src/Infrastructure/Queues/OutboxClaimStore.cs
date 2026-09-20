using Microsoft.EntityFrameworkCore;
using skestock.Application.Common.Interfaces;
using skestock.Application.Queues.Interfaces;

namespace skestock.Infrastructure.Queues;

/// <summary>
/// Persists outbox claim state independently from the hosted publisher. Claiming is deliberately
/// short-lived: the external queue send happens after the transaction commits, so database locks
/// are never held across network I/O.
/// </summary>
public sealed class OutboxClaimStore(IApplicationDbContext dbContext) : IOutboxClaimStore
{
    public async Task<OutboxClaimBatch> ClaimBatchAsync(
        OutboxClaimOptions options,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        var claimId = Guid.NewGuid();
        var executionStrategy = dbContext.Database.CreateExecutionStrategy();

        return await executionStrategy.ExecuteAsync(
            async strategyCancellationToken =>
            {
                await using var transaction =
                    await dbContext.Database.BeginTransactionAsync(strategyCancellationToken);

                var candidateIds = await dbContext.OutboxMessages
                    .Where(x => x.ProcessedAtUtc == null
                                && x.RetryCount < options.MaxRetries
                                && (x.ClaimedUntilUtc == null || x.ClaimedUntilUtc <= nowUtc))
                    .OrderBy(x => x.CreatedAtUtc)
                    .Take(options.BatchSize)
                    .Select(x => x.Id)
                    .ToListAsync(strategyCancellationToken);

                if (candidateIds.Count == 0)
                {
                    await transaction.CommitAsync(strategyCancellationToken);
                    return new OutboxClaimBatch(claimId, []);
                }

                // The eligibility predicate is repeated on the update. If another publisher won
                // the race after the candidate read, SQL Server updates zero rows for that message.
                await dbContext.OutboxMessages
                    .Where(x => candidateIds.Contains(x.Id)
                                && x.ProcessedAtUtc == null
                                && x.RetryCount < options.MaxRetries
                                && (x.ClaimedUntilUtc == null || x.ClaimedUntilUtc <= nowUtc))
                    .ExecuteUpdateAsync(
                        setters => setters
                            .SetProperty(x => x.ClaimId, claimId)
                            .SetProperty(x => x.ClaimedUntilUtc, nowUtc + options.Lease),
                        strategyCancellationToken);

                var messages = await dbContext.OutboxMessages
                    .AsNoTracking()
                    .Where(x => x.ClaimId == claimId)
                    .OrderBy(x => x.CreatedAtUtc)
                    .ToListAsync(strategyCancellationToken);

                await transaction.CommitAsync(strategyCancellationToken);
                return new OutboxClaimBatch(claimId, messages);
            },
            cancellationToken);
    }

    public async Task<bool> MarkPublishedAsync(
        OutboxClaimBatch claim,
        Guid messageId,
        DateTimeOffset processedAtUtc,
        CancellationToken cancellationToken)
    {
        var affectedRows = await dbContext.OutboxMessages
            .Where(x => x.Id == messageId && x.ClaimId == claim.ClaimId && x.ProcessedAtUtc == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.ProcessedAtUtc, processedAtUtc)
                    .SetProperty(x => x.ClaimId, (Guid?)null)
                    .SetProperty(x => x.ClaimedUntilUtc, (DateTimeOffset?)null)
                    .SetProperty(x => x.Error, (string?)null),
                cancellationToken);

        return affectedRows == 1;
    }

    public async Task<int?> RecordFailureAsync(
        OutboxClaimBatch claim,
        Guid messageId,
        string error,
        CancellationToken cancellationToken)
    {
        var affectedRows = await dbContext.OutboxMessages
            .Where(x => x.Id == messageId && x.ClaimId == claim.ClaimId && x.ProcessedAtUtc == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.RetryCount, x => x.RetryCount + 1)
                    .SetProperty(x => x.Error, error)
                    .SetProperty(x => x.ClaimId, (Guid?)null)
                    .SetProperty(x => x.ClaimedUntilUtc, (DateTimeOffset?)null),
                cancellationToken);

        if (affectedRows == 0)
            return null;

        return await dbContext.OutboxMessages
            .Where(x => x.Id == messageId)
            .Select(x => x.RetryCount)
            .SingleAsync(cancellationToken);
    }
}

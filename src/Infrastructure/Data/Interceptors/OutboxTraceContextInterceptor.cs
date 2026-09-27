using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using skestock.Application.Queues;
using skestock.Domain.Queues;

namespace skestock.Infrastructure.Data.Interceptors;

/// <summary>
/// Records the W3C trace context of the operation that adds an <see cref="OutboxMessage"/>, so
/// the publisher and Worker can continue the same distributed trace. Messages that already carry
/// a context are left unchanged.
/// </summary>
public class OutboxTraceContextInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        StampTraceContext(eventData.Context);

        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        StampTraceContext(eventData.Context);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void StampTraceContext(DbContext? context)
    {
        if (context is null) return;

        var (traceParent, traceState) = MessagingTelemetry.CaptureCurrent();
        if (traceParent is null) return;

        foreach (var entry in context.ChangeTracker.Entries<OutboxMessage>())
        {
            if (entry.State != EntityState.Added || entry.Entity.TraceParent is not null) continue;

            entry.Entity.TraceParent = traceParent;
            entry.Entity.TraceState = traceState;
        }
    }
}

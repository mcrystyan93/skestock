using skestock.Domain.Common;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace skestock.Infrastructure.Data.Interceptors;

public class DispatchDomainEventsInterceptor(IMediator mediator) : SaveChangesInterceptor
{
    private readonly List<BaseEvent> _pendingEvents = new();

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        CollectDomainEvents(eventData.Context);

        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        CollectDomainEvents(eventData.Context);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        DispatchDomainEvents().GetAwaiter().GetResult();

        return base.SavedChanges(eventData, result);
    }

    public override async ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        await DispatchDomainEvents();

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    public override void SaveChangesFailed(DbContextErrorEventData eventData)
    {
        _pendingEvents.Clear();

        base.SaveChangesFailed(eventData);
    }

    public override Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        _pendingEvents.Clear();

        return base.SaveChangesFailedAsync(eventData, cancellationToken);
    }

    // Domain events are snapshotted (and cleared off the entities) while changes are being saved,
    // but only published from SavedChanges - i.e. AFTER the transaction commits. This guarantees
    // notification/cache-invalidation handlers (and any read they trigger, including a SignalR-driven
    // client refetch) observe committed data and cannot re-cache stale rows.
    private void CollectDomainEvents(DbContext? context)
    {
        if (context == null) return;

        var entities = context.ChangeTracker
            .Entries<BaseEntity>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity)
            .ToList();

        foreach (var entity in entities)
        {
            _pendingEvents.AddRange(entity.DomainEvents);
            entity.ClearDomainEvents();
        }
    }

    private async Task DispatchDomainEvents()
    {
        if (_pendingEvents.Count == 0) return;

        var events = _pendingEvents.ToList();
        _pendingEvents.Clear();

        foreach (var domainEvent in events)
            await mediator.Publish(domainEvent, CancellationToken.None);
    }
}

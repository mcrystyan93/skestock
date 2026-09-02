using System.ComponentModel.DataAnnotations.Schema;

namespace skestock.Domain.Common;

public abstract class BaseEntity
{
    // GUID v7 (time-ordered) primary key. Values are assigned on insert by the
    // ID-generation SaveChanges interceptor (Guid.CreateVersion7) so keyset pagination
    // ordering on Id stays monotonic.
    public Guid Id { get; set; }

    private readonly List<BaseEvent> _domainEvents = new();

    [NotMapped]
    public IReadOnlyCollection<BaseEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void AddDomainEvent(BaseEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void RemoveDomainEvent(BaseEvent domainEvent)
    {
        _domainEvents.Remove(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}

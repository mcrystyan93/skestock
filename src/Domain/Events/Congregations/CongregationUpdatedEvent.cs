using skestock.Domain.Entities.Congregations;

namespace skestock.Domain.Events.Congregations;

public class CongregationUpdatedEvent(Congregation congregation) : BaseEvent
{
    public Congregation Congregation { get; } = congregation;
}

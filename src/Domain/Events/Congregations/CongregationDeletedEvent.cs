using skestock.Domain.Entities.Congregations;

namespace skestock.Domain.Events.Congregations;

public class CongregationDeletedEvent(Congregation congregation) : BaseEvent
{
    public Congregation Congregation { get; } = congregation;
}

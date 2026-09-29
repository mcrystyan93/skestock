namespace skestock.Domain.Events.OrderList;

public class OrderListCreatedEvent(Guid orderListId, Guid classId) : BaseEvent
{
    public Guid OrderListId { get; } = orderListId;
    public Guid ClassId { get; } = classId;
}

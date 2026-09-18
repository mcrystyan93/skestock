namespace skestock.Domain.Events.OrderList;

public class OrderListCreatedEvent(Guid orderListId) : BaseEvent
{
    public Guid OrderListId { get; } = orderListId;
}

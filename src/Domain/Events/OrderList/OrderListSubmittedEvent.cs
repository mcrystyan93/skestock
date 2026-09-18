namespace skestock.Domain.Events.OrderList;

public class OrderListSubmittedEvent(Guid orderListId) : BaseEvent
{
    public Guid OrderListId { get; } = orderListId;
}

namespace skestock.Domain.Events.OrderList;

public class OrderListReopenedEvent(Guid orderListId) : BaseEvent
{
    public Guid OrderListId { get; } = orderListId;
}

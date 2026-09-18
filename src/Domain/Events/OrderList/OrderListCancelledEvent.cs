namespace skestock.Domain.Events.OrderList;

public class OrderListCancelledEvent(Guid orderListId) : BaseEvent
{
    public Guid OrderListId { get; } = orderListId;
}

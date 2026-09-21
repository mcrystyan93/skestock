using skestock.Domain.Enums;
using skestock.Domain.Events.OrderList;

namespace skestock.Domain.Entities;

public class OrderList : BaseAuditableEntity, IKeysetEntity
{
    public required Guid ClassId { get; set; }
    public SchoolClass Class { get; set; } = null!;

    // Optional human-friendly title and a free-text note for the whole list.
    public string? Name { get; set; }
    public string? Note { get; set; }

    public OrderListStatus Status { get; private set; } = OrderListStatus.Draft;

    // Set when the list is submitted; cleared when a cancelled list is reopened.
    public DateTimeOffset? SubmittedAt { get; private set; }

    public ICollection<OrderListLine> Lines { get; set; } = new List<OrderListLine>();

    public static OrderList Create(Guid classId, string? name, string? note)
    {
        var orderList = new OrderList
        {
            Id = Guid.CreateVersion7(),
            ClassId = classId,
            Name = name,
            Note = note,
            Status = OrderListStatus.Draft
        };

        orderList.AddDomainEvent(new OrderListCreatedEvent(orderList.Id));

        return orderList;
    }

    // True only while the list may still have its metadata/lines edited.
    public bool IsEditable => Status == OrderListStatus.Draft;

    // Only cancelled lists can be returned to the editable draft state.
    public bool IsReopenable => Status == OrderListStatus.Cancelled;

    public void Submit()
    {
        Status = OrderListStatus.Submitted;
        SubmittedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new OrderListSubmittedEvent(Id));
    }

    public void Cancel()
    {
        Status = OrderListStatus.Cancelled;

        AddDomainEvent(new OrderListCancelledEvent(Id));
    }

    // Returns a cancelled list to the editable draft state so it can be approved again.
    public void Reopen()
    {
        Status = OrderListStatus.Draft;
        SubmittedAt = null;

        AddDomainEvent(new OrderListReopenedEvent(Id));
    }
}

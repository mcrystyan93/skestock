namespace skestock.Domain.Entities;

public class OrderListLine : BaseAuditableEntity
{
    public Guid OrderListId { get; set; }
    public OrderList OrderList { get; set; } = null!;

    // Null when the requested product is free text (not yet in the catalog).
    public Guid? ItemId { get; set; }
    public Item? Item { get; set; }

    // Always populated: a snapshot of the catalog item's name when ItemId is set,
    // or the free text the user typed. Protects display even if the Item is later disabled.
    public string ProductName { get; set; } = null!;

    public decimal Quantity { get; set; }

    // Optional unit of measure, mainly useful for free-text lines.
    public string? Unit { get; set; }

    // Free-text observations for this line.
    public string? Notes { get; set; }
}

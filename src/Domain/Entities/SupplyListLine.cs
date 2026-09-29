namespace skestock.Domain.Entities;

public class SupplyListLine : BaseAuditableEntity
{
    public Guid SupplyListId { get; set; }
    public SupplyList SupplyList { get; set; } = null!;

    public Guid ItemId { get; set; }
    public Item Item { get; set; } = null!;

    public decimal Quantity { get; set; } = 1;
    public string Unit { get; set; } = null!;
    public string? Notes { get; set; }
}

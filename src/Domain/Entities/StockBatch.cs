namespace skestock.Domain.Entities;

public class StockBatch: BaseAuditableEntity
{
    public int ItemId { get; set; }
    public Item Item { get; set; } = null!;

    public int LocationId { get; set; }
    public Location Location { get; set; } = null!;

    public int ReceivedClassId { get; set; }
    public SchoolClass ReceivedClass { get; set; } = null!;

    public int Quantity { get; set; }          // remaining quantity in this batch
    public DateOnly? ExpiryDate { get; set; }   // null when Item.IsPerishable == false
    public DateOnly ReceivedDate { get; set; }
    
    public int? GoodsReceiptId { get; set; } // null for batches created outside a receipt (e.g. rollover)
    public GoodsReceipt? GoodsReceipt { get; set; }

    public ICollection<StockTransaction> Transactions { get; set; } = new List<StockTransaction>();
}

namespace skestock.Domain.Entities;

public class GoodsReceipt: BaseAuditableEntity, IKeysetEntity
{
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    public required int ClassId { get; set; }
    public required SchoolClass Class { get; set; }
    public string? SupplierReference { get; set; } // PO number, invoice ref, free text
    public required string Note { get; set; }    
    // Sum of (Quantity * UnitPrice) across all batches in this receipt, captured at
    // receipt time. Stored rather than computed on the fly so later stock adjustments
    // (write-offs, usage) never retroactively change what was actually paid for.
    public decimal TotalAmount { get; set; }

    
    // One receipt -> many batches (one per line item) and their matching transactions
    public ICollection<StockBatch> Batches { get; set; } = new List<StockBatch>();
    public ICollection<StockTransaction> Transactions { get; set; } = new List<StockTransaction>();
}

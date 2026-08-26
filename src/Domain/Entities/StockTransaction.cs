using skestock.Domain.Enums;

namespace skestock.Domain.Entities;

public class StockTransaction: BaseAuditableEntity
{

    public int ItemId { get; set; }
    public Item Item { get; set; } = null!;

    public int LocationId { get; set; }
    public Location Location { get; set; } = null!;

    public int? BatchId { get; set; }
    public StockBatch? Batch { get; set; }

    public int ClassId { get; set; }
    public SchoolClass Class { get; set; } = null!;

    public int UserId { get; set; }
    public UserProfile User { get; set; } = null!;

    public StockTransactionType Type { get; set; }
    public int QuantityChange { get; set; } // positive or negative
    public string? Reason { get; set; }      // e.g. "expired", "damaged", "restock"
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public int? GoodsReceiptId { get; set; } // null for transactions not tied to a bulk receipt
    public GoodsReceipt? GoodsReceipt { get; set; }

}

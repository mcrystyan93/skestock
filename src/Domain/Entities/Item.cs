namespace skestock.Domain.Entities;

public class Item: BaseAuditableEntity, IKeysetEntity
{
    public string? Sku { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string Unit { get; set; } = "unit"; // e.g. "unit", "kg", "box"
    public int MinThreshold { get; set; }
    public bool IsPerishable { get; set; }

    // Optional shelf life in days. When set on a perishable item, a goods-receipt line that omits
    // an expiry date derives it as ReceivedDate + ShelfLifeDays (see the goods-receipt handlers).
    public int? ShelfLifeDays { get; set; }
    public bool IsActive { get; set; } = true;

    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public ICollection<StockBatch> Batches { get; set; } = new List<StockBatch>();
    public ICollection<StockTransaction> Transactions { get; set; } = new List<StockTransaction>();
    public ICollection<ClassBalance> ClassBalances { get; set; } = new List<ClassBalance>();
    
}

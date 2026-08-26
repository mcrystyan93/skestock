namespace skestock.Domain.Entities;


public class Location: BaseAuditableEntity, IKeysetEntity
{
    public string Name { get; set; } = null!;
    public string Type { get; set; } = null!; // "Kitchen", "StorageRoom", ...

    public int? ParentLocationId { get; set; }
    public Location? ParentLocation { get; set; }
    public ICollection<Location> ChildLocations { get; set; } = new List<Location>();

    public ICollection<StockBatch> Batches { get; set; } = new List<StockBatch>();
    public ICollection<StockTransaction> Transactions { get; set; } = new List<StockTransaction>();
    public ICollection<ClassBalance> ClassBalances { get; set; } = new List<ClassBalance>();
}

namespace skestock.Domain.Entities;

public class ClassBalance: BaseAuditableEntity
{

    public Guid ClassId { get; set; }
    public SchoolClass Class { get; set; } = null!;

    public Guid ItemId { get; set; }
    public Item Item { get; set; } = null!;

    public Guid LocationId { get; set; }
    public Location Location { get; set; } = null!;

    public int OpeningQty { get; set; }
    public int? ClosingQty { get; set; } // null until the class is closed
}

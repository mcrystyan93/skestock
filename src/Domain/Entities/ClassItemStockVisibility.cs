namespace skestock.Domain.Entities;

public class ClassItemStockVisibility : BaseAuditableEntity
{
    public Guid ClassId { get; set; }
    public Guid ItemId { get; set; }
    public Guid LocationId { get; set; }
    public bool HideWhenZeroStock { get; set; }
}

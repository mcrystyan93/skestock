namespace skestock.Domain.Entities.Items;

public class ItemLocationThreshold : BaseEntity
{
    public Guid ItemId { get; set; }
    public Guid LocationId { get; set; }
    public int MinThreshold { get; set; }
}

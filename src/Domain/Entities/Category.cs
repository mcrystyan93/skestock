namespace skestock.Domain.Entities;

public class Category: BaseAuditableEntity, IKeysetEntity
{
    public string Name { get; set; } = null!;

    public ICollection<Item> Items { get; set; } = new List<Item>();
}

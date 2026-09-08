namespace skestock.Domain.Entities;

public class Category: BaseAuditableEntity, IKeysetEntity
{
    public string Name { get; set; } = null!;

    public CategoryIcon? Icon { get; set; }

    public ICollection<Item> Items { get; set; } = new List<Item>();
}

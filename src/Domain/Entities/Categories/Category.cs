using skestock.Domain.Entities.Items;

namespace skestock.Domain.Entities.Categories;

public class Category: BaseAuditableEntity, IKeysetEntity
{
    public string Name { get; set; } = null!;

    public CategoryIcon? Icon { get; set; }

    public Guid? ParentId { get; set; }
    public Category? Parent { get; set; }
    public ICollection<Category> Children { get; set; } = new List<Category>();

    public ICollection<Item> Items { get; set; } = new List<Item>();
}

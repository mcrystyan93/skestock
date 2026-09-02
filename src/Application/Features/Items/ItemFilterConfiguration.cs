using System.Linq.Expressions;
using skestock.Application.Common.Filtering;
using skestock.Domain.Entities;

namespace skestock.Application.Features.Items;

public class ItemFilterConfiguration: IFilterConfiguration<Item>
{
    public IReadOnlyDictionary<string, FilterField<Item>> Fields { get; } = new Dictionary<string, FilterField<Item>>(StringComparer.OrdinalIgnoreCase)
    {
        ["id"] = new((Expression<Func<Item, Guid>>)(d => d.Id), typeof(Guid)),
        ["sku"] = new((Expression<Func<Item, string?>>)(d => d.Sku), typeof(string)),
        ["name"] = new((Expression<Func<Item, string>>)(d => d.Name), typeof(string)),
        ["unit"] = new((Expression<Func<Item, string>>)(d => d.Unit), typeof(string)),
        ["categoryId"] = new((Expression<Func<Item, Guid>>)(d => d.CategoryId), typeof(Guid)),
        ["isPerishable"] = new((Expression<Func<Item, bool>>)(d => d.IsPerishable), typeof(bool)),
        ["isActive"] = new((Expression<Func<Item, bool>>)(d => d.IsActive), typeof(bool)),
        ["createdDate"] = new((Expression<Func<Item, DateTimeOffset>>)(d => d.CreatedDate), typeof(DateTimeOffset)),
        ["lastModifiedDate"] = new((Expression<Func<Item, DateTimeOffset>>)(d => d.LastModifiedDate), typeof(DateTimeOffset))
    };
}

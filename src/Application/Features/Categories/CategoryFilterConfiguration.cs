using System.Linq.Expressions;
using skestock.Application.Common.Filtering;
using skestock.Domain.Entities;

namespace skestock.Application.Features.Categories;

public class CategoryFilterConfiguration: IFilterConfiguration<Category>
{
    public IReadOnlyDictionary<string, FilterField<Category>> Fields { get; } = new Dictionary<string, FilterField<Category>>(StringComparer.OrdinalIgnoreCase)
    {
        ["id"] = new((Expression<Func<Category, Guid>>)(d => d.Id), typeof(Guid)),
        ["name"] = new((Expression<Func<Category, string>>)(d => d.Name), typeof(string)),
        ["createdDate"] = new((Expression<Func<Category, DateTimeOffset>>)(d => d.CreatedDate), typeof(DateTimeOffset)),
        ["lastModifiedDate"] = new((Expression<Func<Category, DateTimeOffset>>)(d => d.LastModifiedDate), typeof(DateTimeOffset))
    };
}

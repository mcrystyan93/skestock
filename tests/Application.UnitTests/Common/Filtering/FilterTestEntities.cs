using System.Linq.Expressions;
using skestock.Application.Common.Filtering;

namespace skestock.Application.UnitTests.Common.Filtering;

/// <summary>
/// Minimal standalone entity/config pair used to exercise <see cref="FilterQueryBuilder{TEntity}"/>
/// in isolation (LINQ-to-Objects), without depending on EF Core or a real feature.
/// </summary>
public sealed class FilterTestItem
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public decimal Price { get; init; }
    public DateTimeOffset CreatedDate { get; init; }
    public Guid ExternalId { get; init; }
    public int? Rank { get; init; }
}

public sealed class FilterTestItemFilterConfiguration : IFilterConfiguration<FilterTestItem>
{
    public IReadOnlyDictionary<string, FilterField<FilterTestItem>> Fields { get; } =
        new Dictionary<string, FilterField<FilterTestItem>>(StringComparer.OrdinalIgnoreCase)
        {
            ["id"] = new((Expression<Func<FilterTestItem, int>>)(x => x.Id), typeof(int)),
            ["name"] = new((Expression<Func<FilterTestItem, string>>)(x => x.Name), typeof(string)),
            ["isActive"] = new((Expression<Func<FilterTestItem, bool>>)(x => x.IsActive), typeof(bool)),
            ["price"] = new((Expression<Func<FilterTestItem, decimal>>)(x => x.Price), typeof(decimal)),
            ["createdDate"] = new((Expression<Func<FilterTestItem, DateTimeOffset>>)(x => x.CreatedDate), typeof(DateTimeOffset)),
            ["externalId"] = new((Expression<Func<FilterTestItem, Guid>>)(x => x.ExternalId), typeof(Guid)),
            ["rank"] = new((Expression<Func<FilterTestItem, int?>>)(x => x.Rank), typeof(int?))
        };
}

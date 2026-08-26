namespace skestock.Application.Common.Filtering;

/// <summary>
/// Whitelists fields available for filtering on <typeparamref name="TEntity"/>.
/// Each field defines a typed selector so operators can validate and convert values safely.
/// </summary>
public interface IFilterConfiguration<TEntity>
{
    IReadOnlyDictionary<string, FilterField<TEntity>> Fields { get; }
}

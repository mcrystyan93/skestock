using System.Linq.Expressions;

namespace skestock.Application.Common.Keyset;

/// <summary>
/// Builds dynamic OrderBy and ThenBy expressions for keyset pagination.
/// Applies a list of (property, direction) tuples in order to the query.
/// </summary>
public static class OrderByBuilder<TEntity> where TEntity : class
{
    public static IOrderedQueryable<TEntity> ApplyOrderBy(
        IQueryable<TEntity> query,
        List<(string Key, string Direction)> effectiveSort,
        IKeysetSortConfiguration<TEntity> sortConfig)
    {
        IOrderedQueryable<TEntity>? ordered = null;

        foreach (var (key, direction) in effectiveSort)
        {
            var expr = sortConfig.GetPropertyExpression(key);

            // Force a deterministic "nulls last" placement (regardless of direction or provider
            // default) for nullable columns, by ordering on an "is null" indicator ahead of the
            // value itself. This must match the null-handling convention baked into
            // KeysetPredicateBuilder, otherwise the WHERE clause and ORDER BY disagree and rows
            // get skipped or duplicated across pages.
            var realBody = KeysetExpressionHelper.UnwrapConvert(expr.Body);
            if (KeysetExpressionHelper.IsNullableType(realBody.Type))
            {
                var isNullLambda = Expression.Lambda<Func<TEntity, bool>>(
                    Expression.Equal(realBody, Expression.Constant(null, realBody.Type)),
                    expr.Parameters[0]);
                ordered = ordered == null ? query.OrderBy(isNullLambda) : ordered.ThenBy(isNullLambda);
            }

            ordered = direction == "desc"
                ? (ordered == null ? query.OrderByDescending(expr) : ordered.ThenByDescending(expr))
                : (ordered == null ? query.OrderBy(expr) : ordered.ThenBy(expr));
        }

        return ordered ?? query.OrderByDescending(e => EF.Property<Guid>(e, "Id"));
    }
}


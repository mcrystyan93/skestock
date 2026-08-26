using System.Linq.Expressions;

namespace skestock.Application.Common.Keyset;

/// <summary>
/// Shared expression-tree helpers used by <see cref="OrderByBuilder{TEntity}"/> and
/// <see cref="KeysetPredicateBuilder{TEntity}"/> so both build ORDER BY and WHERE clauses from the
/// exact same property-access expressions (<see cref="IKeysetSortConfiguration{TEntity}.GetPropertyExpression"/>),
/// avoiding drift between the two (e.g. a logical sort key that doesn't match the CLR property name).
/// </summary>
internal static class KeysetExpressionHelper
{
    /// <summary>
    /// Strips any <c>Convert</c>/<c>ConvertChecked</c> wrapper nodes (e.g. the boxing conversion
    /// introduced when a lambda body is typed as <c>dynamic</c>/<c>object</c>) to expose the real
    /// underlying member access expression and its true CLR type.
    /// </summary>
    public static Expression UnwrapConvert(Expression body)
    {
        while (body.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked)
            body = ((UnaryExpression)body).Operand;

        return body;
    }

    /// <summary>
    /// True for reference types and <see cref="Nullable{T}"/> value types - i.e. any type whose
    /// values can be null and therefore requires null-aware ordering/comparison handling.
    /// </summary>
    public static bool IsNullableType(Type type) => !type.IsValueType || Nullable.GetUnderlyingType(type) is not null;

    /// <summary>
    /// Rewrites <paramref name="body"/>, replacing every occurrence of <paramref name="oldParameter"/>
    /// with <paramref name="newParameter"/>. Used to combine property-access expressions that were
    /// each produced with their own independent lambda parameter into a single expression tree that
    /// shares one parameter, so multiple key comparisons can be composed without wrapping them in
    /// <see cref="Expression.Invoke(Expression, Expression[])"/> (which EF Core translates poorly).
    /// </summary>
    public static Expression ReplaceParameter(Expression body, ParameterExpression oldParameter, ParameterExpression newParameter)
        => new ParameterReplacer(oldParameter, newParameter).Visit(body);

    private sealed class ParameterReplacer(ParameterExpression oldParameter, ParameterExpression newParameter) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node) =>
            node == oldParameter ? newParameter : base.VisitParameter(node);
    }
}

using System.Linq.Expressions;
using System.Globalization;
using System.Text.Json;

namespace skestock.Application.Common.Keyset;

/// <summary>
/// Builds composite WHERE clauses for keyset pagination cursors.
/// Generates: (A > a0) OR (A = a0 AND B > b0) OR (A = a0 AND B = b0 AND Id > id0)
/// Supports both ascending and descending directions per column, and nullable columns under a
/// "nulls sort last, regardless of direction" convention (matching the ORDER BY guard applied by
/// <see cref="OrderByBuilder{TEntity}"/>): a non-null cursor value on a nullable column also
/// matches any row where the column is null, and a null cursor value contributes no comparison of
/// its own (nothing sorts "after" null) but still participates in the equality chain so deeper
/// tie-breaker keys (e.g. Id) keep working.
/// </summary>
public static class KeysetPredicateBuilder<TEntity> where TEntity : class
{
    public static IQueryable<TEntity> ApplyKeysetPredicate(
        IQueryable<TEntity> query,
        List<(string Key, string Direction)> effectiveSort,
        Dictionary<string, object?> cursorValues,
        IKeysetSortConfiguration<TEntity> sortConfig)
    {
        if (effectiveSort.Count == 0 || cursorValues.Count == 0)
            return query;

        // A single shared parameter is used for every property access so the per-key comparisons
        // and equalities can be composed directly into one expression tree (AndAlso/OrElse) without
        // resorting to Expression.Invoke, which EF Core translates poorly (or not at all) for
        // complex, deeply-nested keyset predicates.
        var param = Expression.Parameter(typeof(TEntity), "e");

        Expression? combinedOr = null;
        var previousEqualities = new List<Expression>();

        foreach (var (key, direction) in effectiveSort)
        {
            // A cursor produced by an older format (or a partially-populated one) may not contain
            // every sort key. Rather than aborting the whole predicate - which would silently drop
            // deeper tie-breakers like Id and corrupt pagination - we just skip this key.
            if (!cursorValues.TryGetValue(key, out var value))
                continue;

            var (propAccess, propertyType) = GetPropertyAccess(key, sortConfig, param);

            if (value is not null)
            {
                var levelComparison = BuildComparisonExpression(propAccess, propertyType, direction, value);
                if (levelComparison is not null)
                {
                    var term = levelComparison;
                    foreach (var eq in previousEqualities)
                        term = Expression.AndAlso(eq, term);

                    combinedOr = combinedOr is null ? term : Expression.OrElse(combinedOr, term);
                }
            }

            // Extend the equality chain for the next (deeper) key regardless of whether this level
            // produced a comparison - this is what correctly handles a null cursor value on a
            // nullable column (no ">"/"<" is possible against null, but "IS NULL" still lets deeper
            // keys narrow down among tied rows).
            var equality = BuildEqualityExpression(propAccess, propertyType, value);
            if (equality is not null)
                previousEqualities.Add(equality);
        }

        if (combinedOr is null)
            return query;

        var predicate = Expression.Lambda<Func<TEntity, bool>>(combinedOr, param);
        return query.Where(predicate);
    }

    /// <summary>
    /// Resolves the property-access expression for <paramref name="key"/> via the sort
    /// configuration (the same source <see cref="OrderByBuilder{TEntity}"/> uses for ORDER BY),
    /// rewritten onto the shared parameter. Using the configuration instead of
    /// <c>typeof(TEntity).GetProperty(key)</c> also fixes cases where the logical sort key differs
    /// from the CLR property name (e.g. "Created" mapping to the <c>CreatedDate</c> property) -
    /// reflection-by-name would silently fail to find the property and drop the predicate entirely.
    /// </summary>
    private static (Expression Access, Type Type) GetPropertyAccess(
        string key,
        IKeysetSortConfiguration<TEntity> sortConfig,
        ParameterExpression sharedParam)
    {
        var expr = sortConfig.GetPropertyExpression(key);
        var body = KeysetExpressionHelper.UnwrapConvert(expr.Body);
        var rewritten = KeysetExpressionHelper.ReplaceParameter(body, expr.Parameters[0], sharedParam);
        return (rewritten, body.Type);
    }

    private static Expression? BuildEqualityExpression(Expression propAccess, Type propertyType, object? value)
    {
        var constant = BuildTypedConstantExpression(value, propertyType);
        if (constant is null)
            return null;

        return Expression.Equal(propAccess, constant, liftToNull: false, method: null);
    }

    private static Expression? BuildComparisonExpression(Expression propAccess, Type propertyType, string direction, object? value)
    {
        if (value is null)
            return null;

        var constant = BuildTypedConstantExpression(value, propertyType);
        if (constant is null)
            return null;

        var nonNullableType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

        Expression comparison;
        if (nonNullableType == typeof(string))
        {
            var compareMethod = typeof(string).GetMethod(nameof(string.Compare), [typeof(string), typeof(string)]);
            if (compareMethod == null)
                return null;

            var compareCall = Expression.Call(compareMethod, propAccess, constant);
            var zero = Expression.Constant(0);

            comparison = direction == "desc"
                ? Expression.LessThan(compareCall, zero)
                : Expression.GreaterThan(compareCall, zero);
        }
        else
        {
            comparison = direction == "desc"
                ? Expression.LessThan(propAccess, constant, liftToNull: false, method: null)
                : Expression.GreaterThan(propAccess, constant, liftToNull: false, method: null);
        }

        if (!KeysetExpressionHelper.IsNullableType(propertyType))
            return comparison;

        // Nulls-last convention: a row whose value is null for this column always sorts after any
        // non-null value (in both asc and desc), so it must also satisfy "column comes after the
        // cursor's non-null value" and be included here.
        var isNull = Expression.Equal(propAccess, Expression.Constant(null, propertyType), liftToNull: false, method: null);
        return Expression.OrElse(comparison, isNull);
    }

    private static Expression? BuildTypedConstantExpression(object? value, Type propertyType)
    {
        if (value is null)
            return Expression.Constant(null, propertyType);

        var nonNullableType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
        var converted = ConvertToType(value, nonNullableType);
        if (converted is null)
            return null;

        if (Nullable.GetUnderlyingType(propertyType) is not null)
        {
            // Lift underlying constant into Nullable<T>
            return Expression.Convert(Expression.Constant(converted, nonNullableType), propertyType);
        }

        return Expression.Constant(converted, propertyType);
    }

    private static object? ConvertToType(object value, Type targetType)
    {
        if (targetType.IsInstanceOfType(value))
            return value;

        if (value is JsonElement json)
            return ConvertJsonElement(json, targetType);

        if (value is string s)
            return ConvertFromString(s, targetType);

        try
        {
            return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
        }
        catch
        {
            return null;
        }
    }

    private static object? ConvertJsonElement(JsonElement json, Type targetType)
    {
        try
        {
            if (targetType == typeof(string))
                return json.ValueKind == JsonValueKind.Null ? null : json.GetString();

            if (targetType == typeof(int))
                return json.GetInt32();

            if (targetType == typeof(long))
                return json.GetInt64();

            if (targetType == typeof(bool))
                return json.GetBoolean();

            if (targetType == typeof(DateTimeOffset))
                return json.GetDateTimeOffset();

            if (targetType == typeof(DateTime))
                return json.GetDateTime();

            if (targetType == typeof(DateOnly))
                return json.ValueKind == JsonValueKind.String && DateOnly.TryParse(json.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateOnly)
                    ? dateOnly
                    : null;

            if (targetType == typeof(Guid))
                return json.GetGuid();

            if (targetType == typeof(decimal))
                return json.GetDecimal();

            if (targetType == typeof(double))
                return json.GetDouble();

            if (targetType == typeof(float))
                return json.GetSingle();

            if (json.ValueKind == JsonValueKind.String)
                return ConvertFromString(json.GetString(), targetType);

            return null;
        }
        catch
        {
            return null;
        }
    }

    private static object? ConvertFromString(string? value, Type targetType)
    {
        if (value is null)
            return null;

        if (targetType == typeof(string))
            return value;

        if (targetType == typeof(int) && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i))
            return i;

        if (targetType == typeof(long) && long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var l))
            return l;

        if (targetType == typeof(bool) && bool.TryParse(value, out var b))
            return b;

        if (targetType == typeof(DateTimeOffset) && DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dto))
            return dto;

        if (targetType == typeof(DateTime) && DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dt))
            return dt;

        if (targetType == typeof(DateOnly) && DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateOnly))
            return dateOnly;

        if (targetType == typeof(Guid) && Guid.TryParse(value, out var g))
            return g;

        if (targetType == typeof(decimal) && decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var dec))
            return dec;

        if (targetType == typeof(double) && double.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var dbl))
            return dbl;

        if (targetType == typeof(float) && float.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var flt))
            return flt;

        return null;
    }
}

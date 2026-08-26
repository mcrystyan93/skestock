using System.Linq.Expressions;
using System.Text.Json;
using System.Globalization;

namespace skestock.Application.Common.Filtering;

/// <summary>
/// Generic filter applier: takes a list of clauses and applies them to an IQueryable.
/// Only whitelisted fields (from <see cref="IFilterConfiguration{TEntity}"/>) are honored.
/// </summary>
public static class FilterQueryBuilder<TEntity>
{
    public static IQueryable<TEntity> Apply(
        IQueryable<TEntity> query,
        IEnumerable<ColumnFilter> clauses,
        IFilterConfiguration<TEntity> config)
    {
        foreach (var filter in clauses)
        {
            if (!config.Fields.TryGetValue(filter.Field, out var field))
                continue;

            var selector = field.Selector;
            var memberType = selector.Body.Type;
            var targetType = Nullable.GetUnderlyingType(memberType) ?? memberType;

            if (filter.Operator == FilterOperator.In)
            {
                var inValues = ConvertToCollection(filter.Value, targetType);
                if (inValues.Count == 0)
                    continue;

                query = ApplyInClause(query, selector, memberType, targetType, inValues);
                continue;
            }

            if (filter.Operator == FilterOperator.Between)
            {
                var rangeValues = ConvertToRange(filter.Value, targetType);
                if (rangeValues is null)
                    continue;

                query = ApplyBetweenClause(query, selector, memberType, targetType, rangeValues.Value.Start, rangeValues.Value.End);
                continue;
            }

            var typedValue = ConvertToType(filter.Value, targetType);
            if (typedValue is null)
                continue;

            query = ApplySingleValueClause(query, selector, memberType, targetType, filter.Operator, typedValue);
        }

        return query;
    }

    private static IQueryable<TEntity> ApplySingleValueClause(
        IQueryable<TEntity> query,
        LambdaExpression selector,
        Type memberType,
        Type targetType,
        FilterOperator op,
        object value)
    {
        var parameter = selector.Parameters[0];
        var body = selector.Body;
        var constant = BuildTypedConstantExpression(value, memberType, targetType);
        if (constant is null)
            return query;

        Expression comparison = op switch
        {
            FilterOperator.Equals => Expression.Equal(body, constant),
            FilterOperator.NotEquals => Expression.NotEqual(body, constant),
            FilterOperator.Contains when targetType == typeof(string) =>
                Expression.Call(body, typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])!, constant),
            FilterOperator.GreaterThan => Expression.GreaterThan(body, constant),
            FilterOperator.LessThan => Expression.LessThan(body, constant),
            _ => throw new NotSupportedException($"Filter operator '{op}' is not supported for field type '{targetType.Name}'.")
        };

        var predicate = Expression.Lambda<Func<TEntity, bool>>(comparison, parameter);
        return query.Where(predicate);
    }

    private static IQueryable<TEntity> ApplyInClause(
        IQueryable<TEntity> query,
        LambdaExpression selector,
        Type memberType,
        Type targetType,
        IReadOnlyList<object?> values)
    {
        var parameter = selector.Parameters[0];
        var body = selector.Body;

        var typedList = CreateTypedList(memberType, targetType, values);
        var containsMethod = typeof(Enumerable)
            .GetMethods()
            .Single(m => m.Name == nameof(Enumerable.Contains) && m.GetParameters().Length == 2)
            .MakeGenericMethod(memberType);

        var containsCall = Expression.Call(
            containsMethod,
            Expression.Constant(typedList),
            body);

        var predicate = Expression.Lambda<Func<TEntity, bool>>(containsCall, parameter);
        return query.Where(predicate);
    }

    private static IQueryable<TEntity> ApplyBetweenClause(
        IQueryable<TEntity> query,
        LambdaExpression selector,
        Type memberType,
        Type targetType,
        object start,
        object end)
    {
        var parameter = selector.Parameters[0];
        var body = selector.Body;
        var comparer = Comparer<object>.Default;

        var normalizedStart = comparer.Compare(start, end) <= 0 ? start : end;
        var normalizedEnd = comparer.Compare(start, end) <= 0 ? end : start;

        var lowerBoundConstant = BuildTypedConstantExpression(normalizedStart, memberType, targetType);
        var upperBoundConstant = BuildTypedConstantExpression(normalizedEnd, memberType, targetType);
        if (lowerBoundConstant is null || upperBoundConstant is null)
            return query;

        var lowerBound = Expression.GreaterThanOrEqual(body, lowerBoundConstant);
        var upperBound = Expression.LessThanOrEqual(body, upperBoundConstant);
        var betweenExpression = Expression.AndAlso(lowerBound, upperBound);

        var predicate = Expression.Lambda<Func<TEntity, bool>>(betweenExpression, parameter);
        return query.Where(predicate);
    }

    private static object? ConvertToType(object? value, Type targetType)
    {
        if (value is null)
            return null;

        if (targetType == typeof(bool))
            return FilterValueParser.TryParseBoolean(value, out var boolValue) ? boolValue : null;

        if (value is JsonElement json)
            return ConvertJsonElement(json, targetType);

        if (targetType.IsInstanceOfType(value))
            return value;

        if (value is string text)
            return ConvertString(text, targetType);

        // Convert.ChangeType doesn't support enums (neither from string nor from the underlying
        // numeric type), so it's handled explicitly here - e.g. ColumnFilter("status", Equals, 1).
        if (targetType.IsEnum && value is sbyte or byte or short or ushort or int or uint or long or ulong)
            return TryConvertNumberToEnum(Convert.ToInt64(value, CultureInfo.InvariantCulture), targetType);

        try
        {
            return Convert.ChangeType(value, targetType);
        }
        catch
        {
            return null;
        }
    }

    private static object? TryConvertNumberToEnum(long value, Type enumType)
    {
        // Enum.IsDefined requires the value's runtime type to match the enum's underlying type
        // exactly (e.g. throws for a boxed long against an int-backed enum), so the value is
        // converted to Enum.ToObject first and definedness is checked on the resulting enum
        // instance instead.
        var underlyingType = Enum.GetUnderlyingType(enumType);
        object converted;
        try
        {
            converted = Convert.ChangeType(value, underlyingType, CultureInfo.InvariantCulture);
        }
        catch
        {
            return null;
        }

        var enumValue = Enum.ToObject(enumType, converted);
        return Enum.IsDefined(enumType, enumValue) ? enumValue : null;
    }

    private static IReadOnlyList<object?> ConvertToCollection(object? value, Type elementType)
    {
        if (value is null)
            return [];

        if (value is JsonElement json && json.ValueKind == JsonValueKind.Array)
        {
            var result = new List<object?>();
            foreach (var item in json.EnumerateArray())
            {
                var converted = ConvertJsonElement(item, elementType);
                if (converted is not null)
                    result.Add(converted);
            }

            return result;
        }

        if (value is string text)
        {
            return text
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(item => ConvertString(item, elementType))
                .Where(item => item is not null)
                .ToList();
        }

        var single = ConvertToType(value, elementType);
        return single is null ? [] : [single];
    }

    private static Expression? BuildTypedConstantExpression(object? value, Type memberType, Type targetType)
    {
        var converted = ConvertToType(value, targetType);
        if (converted is null)
            return null;

        var constant = Expression.Constant(converted, targetType);
        return memberType == targetType ? constant : Expression.Convert(constant, memberType);
    }

    private static (object Start, object End)? ConvertToRange(object? value, Type elementType)
    {
        var values = ConvertToCollection(value, elementType);
        if (values.Count < 2 || values[0] is null || values[1] is null)
            return null;

        return (values[0]!, values[1]!);
    }

    private static object? ConvertJsonElement(JsonElement json, Type targetType)
    {
        try
        {
            if (targetType == typeof(string))
                return json.ValueKind == JsonValueKind.String ? json.GetString() : json.ToString();

            if (targetType == typeof(bool))
                return FilterValueParser.TryParseBoolean(json, out var boolValue) ? boolValue : null;

            if (targetType.IsEnum)
                return json.ValueKind == JsonValueKind.Number && json.TryGetInt64(out var enumNumber)
                    ? TryConvertNumberToEnum(enumNumber, targetType)
                    : Enum.TryParse(targetType, json.GetString(), ignoreCase: true, out var enumValue) ? enumValue : null;

            if (targetType == typeof(int))
                return json.ValueKind == JsonValueKind.Number
                    ? json.GetInt32()
                    : int.TryParse(json.GetString(), out var intValue) ? intValue : null;

            if (targetType == typeof(long))
                return json.ValueKind == JsonValueKind.Number
                    ? json.GetInt64()
                    : long.TryParse(json.GetString(), out var longValue) ? longValue : null;

            if (targetType == typeof(decimal))
                return json.ValueKind == JsonValueKind.Number
                    ? json.GetDecimal()
                    : decimal.TryParse(json.GetString(), out var decimalValue) ? decimalValue : null;

            if (targetType == typeof(double))
                return json.ValueKind == JsonValueKind.Number
                    ? json.GetDouble()
                    : double.TryParse(json.GetString(), out var doubleValue) ? doubleValue : null;

            if (targetType == typeof(DateTimeOffset))
                return DateTimeOffset.TryParse(json.GetString(), out var dtoValue) ? dtoValue : null;

            if (targetType == typeof(DateTime))
                return DateTime.TryParse(json.GetString(), out var dtValue) ? dtValue : null;

            if (targetType == typeof(DateOnly))
                return DateOnly.TryParse(json.GetString(), out var dateOnlyValue) ? dateOnlyValue : null;

            return ConvertString(json.ToString(), targetType);
        }
        catch
        {
            return null;
        }
    }

    private static object? ConvertString(string value, Type targetType)
    {
        if (targetType == typeof(string))
            return value;
        if (targetType == typeof(bool))
            return FilterValueParser.TryParseBoolean(value, out var boolValue) ? boolValue : null;
        if (targetType.IsEnum)
            return Enum.TryParse(targetType, value, ignoreCase: true, out var enumValue) ? enumValue : null;
        if (targetType == typeof(int))
            return int.TryParse(value, out var intValue) ? intValue : null;
        if (targetType == typeof(long))
            return long.TryParse(value, out var longValue) ? longValue : null;
        if (targetType == typeof(decimal))
            return decimal.TryParse(value, out var decimalValue) ? decimalValue : null;
        if (targetType == typeof(double))
            return double.TryParse(value, out var doubleValue) ? doubleValue : null;
        if (targetType == typeof(DateTimeOffset))
            return DateTimeOffset.TryParse(value, out var dtoValue) ? dtoValue : null;
        if (targetType == typeof(DateTime))
            return DateTime.TryParse(value, out var dtValue) ? dtValue : null;
        if (targetType == typeof(DateOnly))
            return DateOnly.TryParse(value, out var dateOnlyValue) ? dateOnlyValue : null;
        if (targetType == typeof(Guid))
            return Guid.TryParse(value, out var guidValue) ? guidValue : null;

        try
        {
            return Convert.ChangeType(value, targetType);
        }
        catch
        {
            return null;
        }
    }

    private static object CreateTypedList(Type memberType, Type elementType, IReadOnlyList<object?> values)
    {
        var listType = typeof(List<>).MakeGenericType(memberType);
        var list = (System.Collections.IList)Activator.CreateInstance(listType)!;

        foreach (var value in values)
            list.Add(value is null ? null : ConvertToType(value, elementType));

        return list;
    }
}

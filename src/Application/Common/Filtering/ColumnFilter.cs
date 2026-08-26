namespace skestock.Application.Common.Filtering;

public sealed record ColumnFilter(string Field, FilterOperator Operator, object? Value);



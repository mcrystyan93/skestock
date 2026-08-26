using System.Linq.Expressions;

namespace skestock.Application.Common.Filtering;

public sealed record FilterField<TEntity>(LambdaExpression Selector, Type ValueType);

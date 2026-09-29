using System.Linq.Expressions;
using NUnit.Framework;
using Shouldly;
using skestock.Application.Common.Keyset;
using skestock.Application.Features.Categories;
using skestock.Application.Features.GoodsReceipts;
using skestock.Application.Features.Items;
using skestock.Application.Features.Locations;
using skestock.Application.Features.OrderLists;
using skestock.Application.Features.SchoolClasses;
using skestock.Application.Features.StockBatches;

namespace skestock.Application.UnitTests.Common.Keyset;

/// <summary>
/// Guards against sort keys that map to a property name the configuration's expression or the
/// cursor projection does not know (e.g. "LastModified" vs "LastModifiedDate"), which silently
/// falls back to sorting by Id and encodes a null cursor value.
/// </summary>
public class SortConfigurationConsistencyTests
{
    private static IEnumerable<TestCaseData> Configurations()
    {
        yield return Case(new CategorySortConfiguration(), typeof(CategoryCursor));
        yield return Case(new CategoryImportBatchSortConfiguration(), typeof(CategoryImportBatchCursor));
        yield return Case(new GoodsReceiptSortConfiguration(), typeof(GoodsReceiptCursor));
        yield return Case(new GoodsReceiptImportSortConfiguration(), typeof(GoodsReceiptImportCursor));
        yield return Case(new ItemSortConfiguration(), typeof(ItemCursor));
        yield return Case(new ItemImportBatchSortConfiguration(), typeof(ItemImportBatchCursor));
        yield return Case(new LocationSortConfiguration(), typeof(LocationCursor));
        yield return Case(new OrderListSortConfiguration(), typeof(OrderListCursor));
        yield return Case(new SchoolClassSortConfiguration(), typeof(SchoolClassCursor));
        yield return Case(new StockBatchSortConfiguration(), typeof(StockBatchCursor));
    }

    private static TestCaseData Case<TEntity>(IKeysetSortConfiguration<TEntity> config, Type cursorType)
        where TEntity : class
    {
        var properties = config.AllowedSortKeys.Values.SelectMany(p => p)
            .Concat(config.DefaultSort.Select(s => s.Key))
            .Distinct()
            .ToList();
        var expressionMembers = properties.ToDictionary(p => p, p => MemberName(config.GetPropertyExpression(p)));

        return new TestCaseData(properties, expressionMembers, cursorType).SetName($"{config.GetType().Name}");
    }

    [TestCaseSource(nameof(Configurations))]
    public void EverySortPropertyShouldMapToItselfAndExistOnCursor(
        List<string> properties, Dictionary<string, string?> expressionMembers, Type cursorType)
    {
        foreach (var property in properties)
        {
            expressionMembers[property].ShouldBe(property, $"GetPropertyExpression(\"{property}\") falls back to another member");
            cursorType.GetProperty(property).ShouldNotBeNull($"{cursorType.Name} is missing sort property {property}");
        }
    }

    private static string? MemberName(LambdaExpression expression)
    {
        var body = expression.Body is UnaryExpression { NodeType: ExpressionType.Convert } convert
            ? convert.Operand
            : expression.Body;
        return (body as MemberExpression)?.Member.Name;
    }
}

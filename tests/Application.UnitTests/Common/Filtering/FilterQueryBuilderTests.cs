using System.Text.Json;
using skestock.Application.Common.Filtering;
using NUnit.Framework;
using Shouldly;

namespace skestock.Application.UnitTests.Common.Filtering;

public class FilterQueryBuilderTests
{
    private static readonly FilterTestItemFilterConfiguration Config = new();

    private static List<FilterTestItem> Items => new()
    {
        new()
        {
            Id = 1, Name = "Alpha", IsActive = true, Price = 10.5m,
            CreatedDate = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero),
            ExternalId = Guid.Parse("11111111-1111-1111-1111-111111111111"), Rank = 1
        },
        new()
        {
            Id = 2, Name = "Beta", IsActive = false, Price = 20.0m,
            CreatedDate = new DateTimeOffset(2024, 2, 1, 0, 0, 0, TimeSpan.Zero),
            ExternalId = Guid.Parse("22222222-2222-2222-2222-222222222222"), Rank = null
        },
        new()
        {
            Id = 3, Name = "Gamma", IsActive = true, Price = 30.0m,
            CreatedDate = new DateTimeOffset(2024, 3, 1, 0, 0, 0, TimeSpan.Zero),
            ExternalId = Guid.Parse("33333333-3333-3333-3333-333333333333"), Rank = 3
        }
    };

    private static List<int> Apply(params ColumnFilter[] filters) =>
        FilterQueryBuilder<FilterTestItem>.Apply(Items.AsQueryable(), filters, Config)
            .Select(x => x.Id)
            .ToList();

    [Test]
    public void Apply_WithEqualsOperator_FiltersMatchingRows()
    {
        Apply(new ColumnFilter("id", FilterOperator.Equals, 2)).ShouldBe([2]);
    }

    [Test]
    public void Apply_WithNotEqualsOperator_ExcludesMatchingRows()
    {
        Apply(new ColumnFilter("isActive", FilterOperator.NotEquals, true)).ShouldBe([2]);
    }

    [Test]
    public void Apply_WithContainsOperator_FiltersStringContains()
    {
        Apply(new ColumnFilter("name", FilterOperator.Contains, "amm")).ShouldBe([3]); // "Gamma"
    }

    [Test]
    public void Apply_WithContainsOperatorOnNonStringField_ThrowsNotSupportedException()
    {
        Should.Throw<NotSupportedException>(() => Apply(new ColumnFilter("id", FilterOperator.Contains, "1")));
    }

    [Test]
    public void Apply_WithGreaterThanOperator_FiltersDecimalField()
    {
        Apply(new ColumnFilter("price", FilterOperator.GreaterThan, 15m)).ShouldBe([2, 3]);
    }

    [Test]
    public void Apply_WithLessThanOperator_FiltersDateTimeOffsetField()
    {
        var cutoff = new DateTimeOffset(2024, 2, 15, 0, 0, 0, TimeSpan.Zero);
        Apply(new ColumnFilter("createdDate", FilterOperator.LessThan, cutoff)).ShouldBe([1, 2]);
    }

    [Test]
    public void Apply_WithInOperatorAsCommaSeparatedString_FiltersMatchingSet()
    {
        Apply(new ColumnFilter("id", FilterOperator.In, "1,3")).ShouldBe([1, 3]);
    }

    [Test]
    public void Apply_WithInOperatorAsJsonArray_FiltersMatchingSet()
    {
        using var doc = JsonDocument.Parse("[1,3]");
        Apply(new ColumnFilter("id", FilterOperator.In, doc.RootElement)).ShouldBe([1, 3]);
    }

    [Test]
    public void Apply_WithInOperatorAndNoValues_LeavesQueryUnfiltered()
    {
        Apply(new ColumnFilter("id", FilterOperator.In, "")).ShouldBe([1, 2, 3]);
    }

    [Test]
    public void Apply_WithBetweenOperator_FiltersInclusiveRange()
    {
        Apply(new ColumnFilter("price", FilterOperator.Between, "15,30")).ShouldBe([2, 3]);
    }

    [Test]
    public void Apply_WithBetweenOperatorAndReversedBounds_NormalizesRange()
    {
        // start (30) > end (15): builder must swap them instead of matching nothing.
        Apply(new ColumnFilter("price", FilterOperator.Between, "30,15")).ShouldBe([2, 3]);
    }

    [Test]
    public void Apply_WithBetweenOperatorAndOnlyOneValue_LeavesQueryUnfiltered()
    {
        Apply(new ColumnFilter("price", FilterOperator.Between, "15")).ShouldBe([1, 2, 3]);
    }

    [Test]
    public void Apply_WithUnknownField_IsIgnored()
    {
        Apply(new ColumnFilter("doesNotExist", FilterOperator.Equals, "x")).ShouldBe([1, 2, 3]);
    }

    [Test]
    public void Apply_WithQuotedBooleanString_ParsesCorrectly()
    {
        Apply(new ColumnFilter("isActive", FilterOperator.Equals, "'true'")).ShouldBe([1, 3]);
    }

    [Test]
    public void Apply_WithNumericBooleanString_ParsesCorrectly()
    {
        Apply(new ColumnFilter("isActive", FilterOperator.Equals, "0")).ShouldBe([2]);
    }

    [Test]
    public void Apply_WithNullableFieldEquals_FiltersMatchingRows()
    {
        Apply(new ColumnFilter("rank", FilterOperator.Equals, 3)).ShouldBe([3]);
    }

    [Test]
    public void Apply_WithNullFilterValue_LeavesQueryUnfiltered()
    {
        Apply(new ColumnFilter("rank", FilterOperator.Equals, null)).ShouldBe([1, 2, 3]);
    }

    [Test]
    public void Apply_WithGuidFieldAsJsonStringElement_FiltersMatchingRow()
    {
        using var doc = JsonDocument.Parse("\"22222222-2222-2222-2222-222222222222\"");
        Apply(new ColumnFilter("externalId", FilterOperator.Equals, doc.RootElement)).ShouldBe([2]);
    }

    [Test]
    public void Apply_WithMultipleFilters_CombinesThemWithAnd()
    {
        Apply(
            new ColumnFilter("isActive", FilterOperator.Equals, true),
            new ColumnFilter("price", FilterOperator.GreaterThan, 15m)
        ).ShouldBe([3]);
    }
}

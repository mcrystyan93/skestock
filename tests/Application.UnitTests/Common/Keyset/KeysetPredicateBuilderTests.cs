using skestock.Application.Common.Keyset;
using NUnit.Framework;
using Shouldly;

namespace skestock.Application.UnitTests.Common.Keyset;

public class KeysetPredicateBuilderTests
{
    private static readonly KeysetTestItemSortConfiguration Config = new();

    [Test]
    public void ApplyKeysetPredicate_WithMismatchedLogicalAndClrPropertyNames_PaginatesAllRowsWithoutDuplicatesOrGaps()
    {
        // Regression test: KeysetPredicateBuilder used to resolve columns via
        // typeof(TEntity).GetProperty(key) using the *logical* sort key ("Created"), which doesn't
        // match the CLR property in this test entity either (only "CreatedDate" exists via the
        // interface-mapped GetPropertyExpression). Walking the full default sort end-to-end proves
        // the predicate is now actually built (and filters correctly) instead of silently no-op'ing.
        var baseTime = DateTimeOffset.UtcNow;
        var items = Enumerable.Range(1, 25)
            .Select(i => new KeysetTestItem { Id = i, CreatedDate = baseTime.AddSeconds(-i) })
            .ToList();

        var effectiveSort = DynamicSortBuilder<KeysetTestItem>.BuildEffectiveSort([], Config);

        var seenIds = PaginateAll(items, effectiveSort, pageSize: 5);

        // CreatedDate descending == Id ascending here (CreatedDate = baseTime - i)
        seenIds.ShouldBe(Enumerable.Range(1, 25).ToList());
        seenIds.Distinct().Count().ShouldBe(25);
    }

    [Test]
    public void ApplyKeysetPredicate_WithNullableSortColumn_SortsNullsLastWithoutDuplicatesOrGaps()
    {
        var baseTime = DateTimeOffset.UtcNow;
        // Even ids get a Priority value, odd ids are null.
        var items = Enumerable.Range(1, 25)
            .Select(i => new KeysetTestItem { Id = i, CreatedDate = baseTime.AddSeconds(-i), Priority = i % 2 == 0 ? i : null })
            .ToList();

        var effectiveSort = DynamicSortBuilder<KeysetTestItem>.BuildEffectiveSort(
            [new() { Key = "priority", Value = "asc" }], Config);

        var seenIds = PaginateAll(items, effectiveSort, pageSize: 4);

        seenIds.Count.ShouldBe(25);
        seenIds.Distinct().Count().ShouldBe(25);

        var nonNullExpected = Enumerable.Range(1, 12).Select(k => k * 2).ToList(); // priority asc: 2,4,...,24
        seenIds.Take(nonNullExpected.Count).ShouldBe(nonNullExpected);

        var nullTail = seenIds.Skip(nonNullExpected.Count).ToList();
        nullTail.ShouldAllBe(id => id % 2 != 0);
        nullTail.ShouldBe(nullTail.OrderBy(x => x).ToList()); // Id tie-breaker ascending among nulls
    }

    [Test]
    public void ApplyKeysetPredicate_WithNullableSortColumnDescending_StillSortsNullsLast()
    {
        var baseTime = DateTimeOffset.UtcNow;
        var items = Enumerable.Range(1, 25)
            .Select(i => new KeysetTestItem { Id = i, CreatedDate = baseTime.AddSeconds(-i), Priority = i % 2 == 0 ? i : null })
            .ToList();

        var effectiveSort = DynamicSortBuilder<KeysetTestItem>.BuildEffectiveSort(
            [new() { Key = "priority", Value = "desc" }], Config);

        var seenIds = PaginateAll(items, effectiveSort, pageSize: 4);

        seenIds.Count.ShouldBe(25);
        seenIds.Distinct().Count().ShouldBe(25);

        var nonNullExpected = Enumerable.Range(1, 12).Select(k => k * 2).OrderDescending().ToList(); // 24,22,...,2
        seenIds.Take(nonNullExpected.Count).ShouldBe(nonNullExpected);

        var nullTail = seenIds.Skip(nonNullExpected.Count).ToList();
        nullTail.ShouldAllBe(id => id % 2 != 0); // nulls still last even though the column sorts desc
    }

    [Test]
    public void ApplyKeysetPredicate_WithNoCursorValues_ReturnsQueryUnchanged()
    {
        var items = new List<KeysetTestItem> { new() { Id = 1, CreatedDate = DateTimeOffset.UtcNow } }.AsQueryable();
        var effectiveSort = DynamicSortBuilder<KeysetTestItem>.BuildEffectiveSort([], Config);

        var result = KeysetPredicateBuilder<KeysetTestItem>.ApplyKeysetPredicate(
            items, effectiveSort, new Dictionary<string, object?>(), Config);

        result.ShouldBeSameAs(items);
    }

    [Test]
    public void ApplyKeysetPredicate_WithUnknownCursorKeys_DoesNotThrowAndIgnoresThem()
    {
        var items = new List<KeysetTestItem>
        {
            new() { Id = 1, CreatedDate = DateTimeOffset.UtcNow },
            new() { Id = 2, CreatedDate = DateTimeOffset.UtcNow }
        }.AsQueryable();

        var effectiveSort = DynamicSortBuilder<KeysetTestItem>.BuildEffectiveSort([], Config);
        var cursorValues = new Dictionary<string, object?> { ["SomeUnrelatedKey"] = "whatever" };

        Should.NotThrow(() => KeysetPredicateBuilder<KeysetTestItem>.ApplyKeysetPredicate(
            items, effectiveSort, cursorValues, Config).ToList());
    }

    /// <summary>
    /// Simulates a full keyset pagination walk (as GetAllCategoriesHandler does), returning the ids
    /// in the order they were returned across all pages.
    /// </summary>
    private static List<int> PaginateAll(
        List<KeysetTestItem> items,
        List<(string Key, string Direction)> effectiveSort,
        int pageSize)
    {
        var seenIds = new List<int>();
        string? cursor = null;

        for (var page = 0; page < 50; page++)
        {
            var cursorState = CursorCodec<KeysetTestItem>.Decode(cursor);
            IQueryable<KeysetTestItem> query = items.AsQueryable();

            if (cursorState?.KeyValues.Count > 0)
                query = KeysetPredicateBuilder<KeysetTestItem>.ApplyKeysetPredicate(query, effectiveSort, cursorState.KeyValues, Config);

            var pageItems = OrderByBuilder<KeysetTestItem>.ApplyOrderBy(query, effectiveSort, Config).Take(pageSize).ToList();
            if (pageItems.Count == 0)
                break;

            seenIds.AddRange(pageItems.Select(x => x.Id));
            cursor = CursorCodec<KeysetTestItem>.Encode(pageItems[^1], effectiveSort, Config);
        }

        return seenIds;
    }
}

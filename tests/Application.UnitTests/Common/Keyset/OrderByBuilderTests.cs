using skestock.Application.Common.Keyset;
using NUnit.Framework;
using Shouldly;

namespace skestock.Application.UnitTests.Common.Keyset;

public class OrderByBuilderTests
{
    private static readonly KeysetTestItemSortConfiguration Config = new();

    [Test]
    public void ApplyOrderBy_SortsAscendingByRequestedKey()
    {
        var items = new List<KeysetTestItem>
        {
            new() { Id = 3, CreatedDate = DateTimeOffset.UtcNow },
            new() { Id = 1, CreatedDate = DateTimeOffset.UtcNow },
            new() { Id = 2, CreatedDate = DateTimeOffset.UtcNow }
        }.AsQueryable();

        var ordered = OrderByBuilder<KeysetTestItem>.ApplyOrderBy(items, [("Id", "asc")], Config)
            .Select(x => x.Id)
            .ToList();

        ordered.ShouldBe([1, 2, 3]);
    }

    [Test]
    public void ApplyOrderBy_SortsDescendingByRequestedKey()
    {
        var items = new List<KeysetTestItem>
        {
            new() { Id = 3, CreatedDate = DateTimeOffset.UtcNow },
            new() { Id = 1, CreatedDate = DateTimeOffset.UtcNow },
            new() { Id = 2, CreatedDate = DateTimeOffset.UtcNow }
        }.AsQueryable();

        var ordered = OrderByBuilder<KeysetTestItem>.ApplyOrderBy(items, [("Id", "desc")], Config)
            .Select(x => x.Id)
            .ToList();

        ordered.ShouldBe([3, 2, 1]);
    }

    [Test]
    public void ApplyOrderBy_WithMultipleKeys_AppliesThenByInOrder()
    {
        var now = DateTimeOffset.UtcNow;
        var items = new List<KeysetTestItem>
        {
            new() { Id = 2, CreatedDate = now, Priority = 1 },
            new() { Id = 1, CreatedDate = now, Priority = 1 },
            new() { Id = 3, CreatedDate = now, Priority = 0 }
        }.AsQueryable();

        var ordered = OrderByBuilder<KeysetTestItem>.ApplyOrderBy(items, [("Priority", "asc"), ("Id", "asc")], Config)
            .Select(x => x.Id)
            .ToList();

        ordered.ShouldBe([3, 1, 2]);
    }

    [Test]
    public void ApplyOrderBy_WithNullableColumnAscending_PlacesNullsLast()
    {
        var now = DateTimeOffset.UtcNow;
        var items = new List<KeysetTestItem>
        {
            new() { Id = 1, CreatedDate = now, Priority = 5 },
            new() { Id = 2, CreatedDate = now, Priority = null },
            new() { Id = 3, CreatedDate = now, Priority = 1 }
        }.AsQueryable();

        var ordered = OrderByBuilder<KeysetTestItem>.ApplyOrderBy(items, [("Priority", "asc"), ("Id", "asc")], Config)
            .Select(x => x.Id)
            .ToList();

        ordered.ShouldBe([3, 1, 2]); // 1 < 5 < null
    }

    [Test]
    public void ApplyOrderBy_WithNullableColumnDescending_StillPlacesNullsLast()
    {
        var now = DateTimeOffset.UtcNow;
        var items = new List<KeysetTestItem>
        {
            new() { Id = 1, CreatedDate = now, Priority = 5 },
            new() { Id = 2, CreatedDate = now, Priority = null },
            new() { Id = 3, CreatedDate = now, Priority = 1 }
        }.AsQueryable();

        var ordered = OrderByBuilder<KeysetTestItem>.ApplyOrderBy(items, [("Priority", "desc"), ("Id", "asc")], Config)
            .Select(x => x.Id)
            .ToList();

        // 5 > 1 in descending order, but null must still sort after both, never before.
        ordered.ShouldBe([1, 3, 2]);
    }
}

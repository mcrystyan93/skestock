using skestock.Application.Common.Exceptions;
using skestock.Application.Common.Filtering;
using skestock.Application.Common.Models;
using skestock.Application.Features.Items.Models;
using skestock.Application.Features.Items.Queries.GetAllItems;
using skestock.Domain.Entities;

namespace skestock.Application.FunctionalTests.Features.Items.Queries.GetAllItems;

public class GetAllItemsQueryTests : TestBase
{
    /// <summary>
    /// Every test seeds items tagged with its own unique <see cref="_prefix"/> and always filters
    /// on it via <c>SearchTerm</c>. This keeps assertions correct regardless of leftover rows
    /// from other tests/fixtures, and - just as importantly - keeps each test's cache key (which
    /// incorporates the search term) distinct, so cached results from previous tests against the
    /// same long-lived in-process HybridCache instance can't bleed into this one.
    /// </summary>
    private string _prefix = null!;
    private Category _category = null!;

    [SetUp]
    public async Task SetUpPrefix()
    {
        _prefix = $"FT{Guid.NewGuid():N}"[..10];
        _category = new Category { Name = $"{_prefix}-Category" };
        await TestApp.AddAsync(_category);
    }

    private async Task<List<Item>> SeedItemsAsync(params string[] names)
    {
        var created = new List<Item>();

        foreach (var name in names)
        {
            var item = new Item { Name = $"{_prefix}-{name}", Unit = "unit", CategoryId = _category.Id };
            await TestApp.AddAsync(item);
            created.Add(item);
        }

        return created;
    }

    private static GetAllItemsQuery Query(
        string searchTerm,
        int pageSize = PaginationConstants.DEFAULT_PAGE_SIZE,
        string? cursor = null,
        List<PaginationSort>? sort = null,
        List<ColumnFilter>? filters = null) =>
        new()
        {
            SearchTerm = searchTerm,
            PageSize = pageSize,
            Cursor = cursor,
            Sort = sort ?? [],
            Filters = filters ?? []
        };

    [Test]
    public async Task Handle_ReturnsOnlySeededItemsMatchingSearchTerm()
    {
        await SeedItemsAsync("Alpha", "Beta", "Gamma");

        var result = await TestApp.SendAsync(Query(_prefix));

        result.IsSuccess.ShouldBeTrue();
        result.Value.Data.Count().ShouldBe(3);
        result.Value.Data.Select(i => i.Name).ShouldAllBe(name => name.StartsWith(_prefix));
    }

    [Test]
    public async Task Handle_WithNoMatches_ReturnsEmptyPage()
    {
        var result = await TestApp.SendAsync(Query($"{_prefix}-does-not-exist"));

        result.IsSuccess.ShouldBeTrue();
        result.Value.Data.ShouldBeEmpty();
        result.Value.HasNextPage.ShouldBeFalse();
    }

    [Test]
    public async Task Handle_WithNameSortAscending_ReturnsAlphabeticalOrder()
    {
        await SeedItemsAsync("Charlie", "Alpha", "Bravo");

        var result = await TestApp.SendAsync(Query(
            _prefix,
            sort: [new PaginationSort { Key = "name", Value = "ascend" }]));

        result.IsSuccess.ShouldBeTrue();
        result.Value.Data.Select(i => i.Name).ShouldBe(
            [$"{_prefix}-Alpha", $"{_prefix}-Bravo", $"{_prefix}-Charlie"]);
    }

    [Test]
    public async Task Handle_PagingThroughCursors_ReturnsAllSeededItemsExactlyOnce()
    {
        var names = Enumerable.Range(0, 7).Select(i => $"Item{i:D2}").ToArray();
        await SeedItemsAsync(names);

        var collected = new List<ItemDto>();
        string? cursor = null;
        var safety = 0;

        while (true)
        {
            safety++;
            safety.ShouldBeLessThan(20);

            var result = await TestApp.SendAsync(Query(
                _prefix,
                pageSize: 3,
                cursor: cursor,
                sort: [new PaginationSort { Key = "name", Value = "ascend" }]));

            result.IsSuccess.ShouldBeTrue();
            collected.AddRange(result.Value.Data);

            if (!result.Value.HasNextPage)
                break;

            cursor = result.Value.NextCursor;
        }

        collected.Select(i => i.Id).Distinct().Count().ShouldBe(7);
        collected.Select(i => i.Name).ShouldBe(names.Select(n => $"{_prefix}-{n}").ToList());
    }

    [Test]
    public async Task Handle_WithColumnFilterEquals_ReturnsOnlyMatchingItem()
    {
        var seeded = await SeedItemsAsync("Alpha", "Beta");
        var targetId = seeded.Single(i => i.Name.EndsWith("Beta")).Id;

        var result = await TestApp.SendAsync(Query(
            _prefix,
            filters: [new ColumnFilter("id", FilterOperator.Equals, targetId)]));

        result.IsSuccess.ShouldBeTrue();
        result.Value.Data.Count().ShouldBe(1);
        result.Value.Data.Single().Id.ShouldBe(targetId);
    }

    [Test]
    public async Task Handle_WithInvalidPageSize_ThrowsValidationException()
    {
        await SeedItemsAsync("Alpha");

        var act = async () => await TestApp.SendAsync(Query(_prefix, pageSize: 0));

        var exception = await act.ShouldThrowAsync<ValidationException>();
        exception.Errors.ShouldContainKey(nameof(GetAllItemsQuery.PageSize));
    }

    [Test]
    public async Task Handle_WithMalformedCursor_ThrowsValidationException()
    {
        var act = async () => await TestApp.SendAsync(Query(_prefix, cursor: "not-a-valid-cursor"));

        await act.ShouldThrowAsync<ValidationException>();
    }

    [Test]
    public async Task Handle_WithCursorFromDifferentSort_ThrowsValidationException()
    {
        await SeedItemsAsync("Alpha", "Beta", "Gamma");

        var firstPage = await TestApp.SendAsync(Query(
            _prefix,
            pageSize: 1,
            sort: [new PaginationSort { Key = "name", Value = "ascend" }]));

        firstPage.Value.HasNextPage.ShouldBeTrue();

        var act = async () => await TestApp.SendAsync(Query(
            _prefix,
            cursor: firstPage.Value.NextCursor,
            sort: [new PaginationSort { Key = "name", Value = "descend" }]));

        await act.ShouldThrowAsync<ValidationException>();
    }
}

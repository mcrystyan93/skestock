using FluentResults;
using skestock.Application.Common.Filtering;
using skestock.Application.Common.Models;
using skestock.Application.Features.Categories.Models;
using skestock.Application.Features.Categories.Queries.GetAllCategories;
using skestock.Domain.Entities;

namespace skestock.Application.FunctionalTests.Features.Categories.Queries.GetAllCategories;

public class GetAllCategoriesQueryTests : TestBase
{
    /// <summary>
    /// Every test seeds categories tagged with its own unique <see cref="_prefix"/> and always
    /// filters on it via <c>SearchTerm</c>. This keeps assertions correct regardless of leftover
    /// rows from other tests/fixtures, and - just as importantly - keeps each test's cache key
    /// (which incorporates the search term) distinct, so cached results from previous tests
    /// against the same long-lived in-process HybridCache instance can't bleed into this one.
    /// </summary>
    private string _prefix = null!;

    [SetUp]
    public void SetUpPrefix()
    {
        _prefix = $"FT{Guid.NewGuid():N}"[..10];
    }

    private async Task<List<Category>> SeedCategoriesAsync(params string[] names)
    {
        var created = new List<Category>();

        foreach (var name in names)
        {
            var category = new Category { Name = $"{_prefix}-{name}" };
            await TestApp.AddAsync(category);
            created.Add(category);
        }

        return created;
    }

    private static GetAllCategoriesQuery Query(
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
    public async Task Handle_ReturnsOnlySeededCategoriesMatchingSearchTerm()
    {
        await SeedCategoriesAsync("Alpha", "Beta", "Gamma");

        var result = await TestApp.SendAsync(Query(_prefix));

        result.IsSuccess.ShouldBeTrue();
        result.Value.Data.Count().ShouldBe(3);
        result.Value.Data.Select(c => c.Name).ShouldAllBe(name => name.StartsWith(_prefix));
    }

    [Test]
    public async Task Handle_ReturnsCategoryIcon()
    {
        var category = new Category
        {
            Name = $"{_prefix}-Stationery",
            Icon = new CategoryIcon("Square Q", "square-q", "/assets/icons/square-q.svg")
        };
        await TestApp.AddAsync(category);

        var result = await TestApp.SendAsync(Query(_prefix));

        result.IsSuccess.ShouldBeTrue();
        var icon = result.Value.Data.Single().Icon;
        icon.ShouldNotBeNull();
        icon!.Name.ShouldBe("Square Q");
        icon.FileName.ShouldBe("square-q");
        icon.Path.ShouldBe("/assets/icons/square-q.svg");
    }

    [Test]
    public async Task Handle_ReturnsNumberOfItemsAssignedToEachCategory()
    {
        var categories = await SeedCategoriesAsync("WithItems", "WithoutItems");
        var categoryWithItems = categories.Single(c => c.Name.EndsWith("WithItems"));

        await TestApp.AddAsync(new Item
        {
            Name = $"{_prefix}-Notebook",
            CategoryId = categoryWithItems.Id
        });
        await TestApp.AddAsync(new Item
        {
            Name = $"{_prefix}-Pen",
            CategoryId = categoryWithItems.Id,
            IsActive = false
        });

        var result = await TestApp.SendAsync(Query(_prefix));

        result.IsSuccess.ShouldBeTrue();
        result.Value.Data.Single(c => c.Name.EndsWith("WithItems")).ItemCount.ShouldBe(2);
        result.Value.Data.Single(c => c.Name.EndsWith("WithoutItems")).ItemCount.ShouldBe(0);
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
        await SeedCategoriesAsync("Charlie", "Alpha", "Bravo");

        var result = await TestApp.SendAsync(Query(
            _prefix,
            sort: [new PaginationSort { Key = "name", Value = "ascend" }]));

        result.IsSuccess.ShouldBeTrue();
        result.Value.Data.Select(c => c.Name).ShouldBe(
            [$"{_prefix}-Alpha", $"{_prefix}-Bravo", $"{_prefix}-Charlie"]);
    }

    [Test]
    public async Task Handle_WithNameSortDescending_ReturnsReverseAlphabeticalOrder()
    {
        await SeedCategoriesAsync("Charlie", "Alpha", "Bravo");

        var result = await TestApp.SendAsync(Query(
            _prefix,
            sort: [new PaginationSort { Key = "name", Value = "descend" }]));

        result.IsSuccess.ShouldBeTrue();
        result.Value.Data.Select(c => c.Name).ShouldBe(
            [$"{_prefix}-Charlie", $"{_prefix}-Bravo", $"{_prefix}-Alpha"]);
    }

    [Test]
    public async Task Handle_PagingThroughCursors_ReturnsAllSeededItemsExactlyOnce()
    {
        var names = Enumerable.Range(0, 7).Select(i => $"Item{i:D2}").ToArray();
        await SeedCategoriesAsync(names);

        var collected = new List<CategoryDto>();
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

        collected.Select(c => c.Id).Distinct().Count().ShouldBe(7);
        collected.Select(c => c.Name).ShouldBe(names.Select(n => $"{_prefix}-{n}").ToList());
    }

    [Test]
    public async Task Handle_WithColumnFilterEquals_ReturnsOnlyMatchingCategory()
    {
        var seeded = await SeedCategoriesAsync("Alpha", "Beta");
        var targetId = seeded.Single(c => c.Name.EndsWith("Beta")).Id;

        var result = await TestApp.SendAsync(Query(
            _prefix,
            filters: [new ColumnFilter("id", FilterOperator.Equals, targetId)]));

        result.IsSuccess.ShouldBeTrue();
        result.Value.Data.Count().ShouldBe(1);
        result.Value.Data.Single().Id.ShouldBe(targetId);
    }

    [Test]
    public async Task Handle_WithColumnFilterContains_FiltersByNameSubstring()
    {
        await SeedCategoriesAsync("AlphaWidget", "BetaWidget", "GammaGadget");

        var result = await TestApp.SendAsync(Query(
            _prefix,
            filters: [new ColumnFilter("name", FilterOperator.Contains, "Widget")]));

        result.IsSuccess.ShouldBeTrue();
        result.Value.Data.Count().ShouldBe(2);
        result.Value.Data.Select(c => c.Name).ShouldAllBe(name => name.Contains("Widget"));
    }

    [Test]
    public async Task Handle_WithInvalidPageSize_ThrowsValidationException()
    {
        await SeedCategoriesAsync("Alpha");

        var act = async () => await TestApp.SendAsync(Query(_prefix, pageSize: 0));

        var exception = await act.ShouldThrowAsync<skestock.Application.Common.Exceptions.ValidationException>();
        exception.Errors.ShouldContainKey(nameof(GetAllCategoriesQuery.PageSize));
    }

    [Test]
    public async Task Handle_WithMalformedCursor_ThrowsValidationException()
    {
        var act = async () => await TestApp.SendAsync(Query(_prefix, cursor: "not-a-valid-cursor"));

        await act.ShouldThrowAsync<skestock.Application.Common.Exceptions.ValidationException>();
    }

    [Test]
    public async Task Handle_WithCursorFromDifferentSort_ThrowsValidationException()
    {
        await SeedCategoriesAsync("Alpha", "Beta", "Gamma");

        var firstPage = await TestApp.SendAsync(Query(
            _prefix,
            pageSize: 1,
            sort: [new PaginationSort { Key = "name", Value = "ascend" }]));

        firstPage.Value.HasNextPage.ShouldBeTrue();

        // Reuse the cursor issued for a "name ascend" sort while requesting a different sort.
        var act = async () => await TestApp.SendAsync(Query(
            _prefix,
            cursor: firstPage.Value.NextCursor,
            sort: [new PaginationSort { Key = "name", Value = "descend" }]));

        await act.ShouldThrowAsync<skestock.Application.Common.Exceptions.ValidationException>();
    }
}

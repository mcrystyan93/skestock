using skestock.Application.Common.Filtering;
using skestock.Application.Common.Models;
using skestock.Application.Features.SchoolClasses.Models;
using skestock.Application.Features.SchoolClasses.Queries.GetAllSchoolClasses;
using skestock.Domain.Entities;
using skestock.Domain.Enums;

namespace skestock.Application.FunctionalTests.Features.SchoolClasses.Queries.GetAllSchoolClasses;

public class GetAllSchoolClassesQueryTests : TestBase
{
    /// <summary>
    /// Every test seeds school classes tagged with its own unique <see cref="_prefix"/> and always
    /// filters on it via <c>SearchTerm</c>. This keeps assertions correct regardless of leftover
    /// rows from other tests/fixtures, and keeps each test's cache key distinct.
    /// </summary>
    private string _prefix = null!;

    [SetUp]
    public void SetUpPrefix()
    {
        _prefix = $"FT{Guid.NewGuid():N}"[..10];
    }

    private async Task<List<SchoolClass>> SeedSchoolClassesAsync(params string[] names)
    {
        var created = new List<SchoolClass>();

        foreach (var name in names)
        {
            var schoolClass = new SchoolClass
            {
                Name = $"{_prefix}-{name}",
                StartDate = new DateOnly(2026, 1, 1),
                EndDate = new DateOnly(2026, 6, 1),
                Status = ClassStatus.Upcoming
            };
            await TestApp.AddAsync(schoolClass);
            created.Add(schoolClass);
        }

        return created;
    }

    private static GetAllSchoolClassesQuery Query(
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
    public async Task Handle_ReturnsOnlySeededSchoolClassesMatchingSearchTerm()
    {
        await SeedSchoolClassesAsync("Alpha", "Beta", "Gamma");

        var result = await TestApp.SendAsync(Query(_prefix));

        result.IsSuccess.ShouldBeTrue();
        result.Value.Data.Count().ShouldBe(3);
        result.Value.Data.Select(c => c.Name).ShouldAllBe(name => name.StartsWith(_prefix));
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
        await SeedSchoolClassesAsync("Charlie", "Alpha", "Bravo");

        var result = await TestApp.SendAsync(Query(
            _prefix,
            sort: [new PaginationSort { Key = "name", Value = "ascend" }]));

        result.IsSuccess.ShouldBeTrue();
        result.Value.Data.Select(c => c.Name).ShouldBe(
            [$"{_prefix}-Alpha", $"{_prefix}-Bravo", $"{_prefix}-Charlie"]);
    }

    [Test]
    public async Task Handle_PagingThroughCursors_ReturnsAllSeededItemsExactlyOnce()
    {
        var names = Enumerable.Range(0, 7).Select(i => $"Item{i:D2}").ToArray();
        await SeedSchoolClassesAsync(names);

        var collected = new List<SchoolClassDto>();
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
    public async Task Handle_WithColumnFilterEquals_ReturnsOnlyMatchingSchoolClass()
    {
        var seeded = await SeedSchoolClassesAsync("Alpha", "Beta");
        var targetId = seeded.Single(c => c.Name.EndsWith("Beta")).Id;

        var result = await TestApp.SendAsync(Query(
            _prefix,
            filters: [new ColumnFilter("id", FilterOperator.Equals, targetId)]));

        result.IsSuccess.ShouldBeTrue();
        result.Value.Data.Count().ShouldBe(1);
        result.Value.Data.Single().Id.ShouldBe(targetId);
    }

    [Test]
    public async Task Handle_WithColumnFilterOnStatus_ReturnsOnlyMatchingSchoolClasses()
    {
        var active = new SchoolClass
        {
            Name = $"{_prefix}-Active",
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 6, 1),
            Status = ClassStatus.Active
        };
        var upcoming = new SchoolClass
        {
            Name = $"{_prefix}-Upcoming",
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 6, 1),
            Status = ClassStatus.Upcoming
        };
        await TestApp.AddAsync(active);
        await TestApp.AddAsync(upcoming);

        var result = await TestApp.SendAsync(Query(
            _prefix,
            filters: [new ColumnFilter("status", FilterOperator.Equals, nameof(ClassStatus.Active))]));

        result.IsSuccess.ShouldBeTrue();
        result.Value.Data.Count().ShouldBe(1);
        result.Value.Data.Single().Id.ShouldBe(active.Id);
    }

    [Test]
    public async Task Handle_WithInvalidPageSize_ThrowsValidationException()
    {
        await SeedSchoolClassesAsync("Alpha");

        var act = async () => await TestApp.SendAsync(Query(_prefix, pageSize: 0));

        var exception = await act.ShouldThrowAsync<skestock.Application.Common.Exceptions.ValidationException>();
        exception.Errors.ShouldContainKey(nameof(GetAllSchoolClassesQuery.PageSize));
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
        await SeedSchoolClassesAsync("Alpha", "Beta", "Gamma");

        var firstPage = await TestApp.SendAsync(Query(
            _prefix,
            pageSize: 1,
            sort: [new PaginationSort { Key = "name", Value = "ascend" }]));

        firstPage.Value.HasNextPage.ShouldBeTrue();

        var act = async () => await TestApp.SendAsync(Query(
            _prefix,
            cursor: firstPage.Value.NextCursor,
            sort: [new PaginationSort { Key = "name", Value = "descend" }]));

        await act.ShouldThrowAsync<skestock.Application.Common.Exceptions.ValidationException>();
    }
}

using skestock.Application.Common.Exceptions;
using skestock.Application.Common.Filtering;
using skestock.Application.Common.Models;
using skestock.Application.Features.OrderLists.Commands.CreateOrderList;
using skestock.Application.Features.OrderLists.Commands.SubmitOrderList;
using skestock.Application.Features.OrderLists.Models;
using skestock.Application.Features.OrderLists.Queries.GetAllOrderLists;
using skestock.Domain.Entities;
using skestock.Domain.Enums;

namespace skestock.Application.FunctionalTests.Features.OrderLists.Queries;

public class GetAllOrderListsQueryTests : TestBase
{
    private string _prefix = null!;
    private SchoolClass _schoolClass = null!;
    private Item _item = null!;

    [SetUp]
    public async Task SetUpPrerequisites()
    {
        _prefix = $"FT{Guid.NewGuid():N}"[..10];

        var category = new Category { Name = $"{_prefix}-Category" };
        await TestApp.AddAsync(category);

        _item = new Item { Name = $"{_prefix}-Rice", Unit = "kg", CategoryId = category.Id };
        await TestApp.AddAsync(_item);

        _schoolClass = new SchoolClass
        {
            Name = $"{_prefix}-Class",
            StartDate = new DateOnly(2024, 1, 1),
            EndDate = new DateOnly(2024, 6, 1)
        };
        await TestApp.AddAsync(_schoolClass);

        var userId = await TestApp.RunAsDefaultUserAsync();
        await TestApp.AddAsync(new UserProfile { IdentityId = userId!.Value, FirstName = "Staff", LastName = "Member" });
    }

    private async Task<Guid> SeedOrderListAsync(string name, bool submit = false)
    {
        var created = await TestApp.SendAsync(new CreateOrderListCommand
        {
            ClassId = _schoolClass.Id,
            Name = $"{_prefix}-{name}",
            Lines = [new OrderListLineInput { ItemId = _item.Id, Quantity = 1 }]
        });

        if (submit)
            await TestApp.SendAsync(new SubmitOrderListCommand { Id = created.Value.Id });

        return created.Value.Id;
    }

    private static GetAllOrderListsQuery Query(
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
    public async Task Handle_ReturnsOnlySeededListsMatchingSearchTerm()
    {
        await SeedOrderListAsync("Weekly");

        var result = await TestApp.SendAsync(Query(_prefix));

        result.IsSuccess.ShouldBeTrue();
        result.Value.Data.Count().ShouldBe(1);
        result.Value.Data.Select(o => o.Name).ShouldAllBe(name => name!.StartsWith(_prefix));
        result.Value.Data.Single().LineCount.ShouldBe(1);
        result.Value.Data.Single().ClassName.ShouldBe(_schoolClass.Name);
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
    public async Task Handle_WithStatusFilter_ReturnsOnlyMatchingLists()
    {
        await SeedOrderListAsync("Draft-One");
        await SeedOrderListAsync("Submitted-One", submit: true);

        var result = await TestApp.SendAsync(Query(
            _prefix,
            filters: [new ColumnFilter("status", FilterOperator.Equals, (int)OrderListStatus.Submitted)]));

        result.IsSuccess.ShouldBeTrue();
        result.Value.Data.Count().ShouldBe(1);
        result.Value.Data.Single().Status.ShouldBe(OrderListStatus.Submitted.ToString());
    }

    [Test]
    public async Task Handle_WithClassIdFilter_ReturnsOnlyMatchingLists()
    {
        await SeedOrderListAsync("Mine");

        var result = await TestApp.SendAsync(Query(
            _prefix,
            filters: [new ColumnFilter("classId", FilterOperator.Equals, _schoolClass.Id)]));

        result.IsSuccess.ShouldBeTrue();
        result.Value.Data.Count().ShouldBe(1);
        result.Value.Data.Single().ClassId.ShouldBe(_schoolClass.Id);
    }

    [Test]
    public async Task Handle_WithNameSortAscending_ReturnsListsInNameOrder()
    {
        await SeedOrderListAsync("Charlie");
        await SeedOrderListAsync("Alpha");
        await SeedOrderListAsync("Bravo");

        var result = await TestApp.SendAsync(Query(
            _prefix,
            sort: [new PaginationSort { Key = "name", Value = "ascend" }]));

        result.IsSuccess.ShouldBeTrue();
        result.Value.Data.Select(o => o.Name).ShouldBe(
        [
            $"{_prefix}-Alpha",
            $"{_prefix}-Bravo",
            $"{_prefix}-Charlie"
        ]);
    }

    [Test]
    public async Task Handle_PagingThroughCursors_ReturnsAllSeededListsExactlyOnce()
    {
        for (var i = 0; i < 7; i++)
            await SeedOrderListAsync($"Item{i:D2}");

        var collected = new List<OrderListListItemDto>();
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
                sort: [new PaginationSort { Key = "id", Value = "ascend" }]));

            result.IsSuccess.ShouldBeTrue();
            collected.AddRange(result.Value.Data);

            if (!result.Value.HasNextPage)
                break;

            cursor = result.Value.NextCursor;
        }

        collected.Select(o => o.Id).Distinct().Count().ShouldBe(7);
    }

    [Test]
    public async Task Handle_WithInvalidPageSize_ThrowsValidationException()
    {
        await SeedOrderListAsync("Salt");

        var act = async () => await TestApp.SendAsync(Query(_prefix, pageSize: 0));

        var exception = await act.ShouldThrowAsync<ValidationException>();
        exception.Errors.ShouldContainKey(nameof(GetAllOrderListsQuery.PageSize));
    }

    [Test]
    public async Task Handle_WithMalformedCursor_ThrowsValidationException()
    {
        var act = async () => await TestApp.SendAsync(Query(_prefix, cursor: "not-a-valid-cursor"));

        await act.ShouldThrowAsync<ValidationException>();
    }
}

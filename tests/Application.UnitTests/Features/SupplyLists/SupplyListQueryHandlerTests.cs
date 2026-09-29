using NUnit.Framework;
using Shouldly;
using skestock.Application.Common.Errors;
using skestock.Application.Common.Filtering;
using skestock.Application.Features.SupplyLists.Queries.GetAllSupplyLists;
using skestock.Application.Features.SupplyLists.Queries.GetSupplyListById;
using skestock.Domain.Entities;
using skestock.Domain.Enums;

namespace skestock.Application.UnitTests.Features.SupplyLists;

public class SupplyListQueryHandlerTests
{
    private static async Task<SupplyListTestDbContext> SeedAsync(int count)
    {
        var (context, rice, _, _) = await SupplyListTestData.CreateContextAsync();
        for (var i = 1; i <= count; i++)
        {
            var list = new SupplyList
            {
                Name = $"List {i:00}",
                Frequency = i % 2 == 0 ? SupplyListFrequency.Weekly : SupplyListFrequency.Once,
                IsActive = i != 3
            };
            list.Lines.Add(new SupplyListLine { ItemId = rice.Id, Unit = "kg" });
            context.SupplyLists.Add(list);
        }
        await context.SaveChangesAsync();
        return context;
    }

    [Test]
    public async Task GetById_ReturnsListWithLines()
    {
        await using var context = await SeedAsync(1);
        var id = context.SupplyLists.Single().Id;

        var result = await new GetSupplyListByIdHandler(context).Handle(
            new GetSupplyListByIdQuery { Id = id }, CancellationToken.None);

        result.Value.Lines.ShouldHaveSingleItem().ItemName.ShouldBe("Rice");
    }

    [Test]
    public async Task GetById_FailsWhenMissing()
    {
        await using var context = await SeedAsync(0);

        var result = await new GetSupplyListByIdHandler(context).Handle(
            new GetSupplyListByIdQuery { Id = Guid.NewGuid() }, CancellationToken.None);

        result.Errors.ShouldContain(e => e is SupplyListErrors.SupplyListNotFound);
    }

    [Test]
    public async Task GetAll_PaginatesWithCursor()
    {
        await using var context = await SeedAsync(5);
        var handler = new GetAllSupplyListsHandler(context);

        var first = await handler.Handle(new GetAllSupplyListsQuery { PageSize = 2 }, CancellationToken.None);
        first.Value.Data.Select(d => d.Name).ShouldBe(["List 01", "List 02"]);
        first.Value.HasNextPage.ShouldBeTrue();
        first.Value.Data.First().LineCount.ShouldBe(1);

        var second = await handler.Handle(
            new GetAllSupplyListsQuery { PageSize = 2, Cursor = first.Value.NextCursor }, CancellationToken.None);
        second.Value.Data.Select(d => d.Name).ShouldBe(["List 03", "List 04"]);

        var last = await handler.Handle(
            new GetAllSupplyListsQuery { PageSize = 2, Cursor = second.Value.NextCursor }, CancellationToken.None);
        last.Value.Data.Select(d => d.Name).ShouldBe(["List 05"]);
        last.Value.HasNextPage.ShouldBeFalse();
    }

    [Test]
    public async Task GetAll_FiltersAndSearches()
    {
        await using var context = await SeedAsync(5);
        var handler = new GetAllSupplyListsHandler(context);

        var inactive = await handler.Handle(new GetAllSupplyListsQuery
        {
            Filters = [new ColumnFilter("isActive", FilterOperator.Equals, false)]
        }, CancellationToken.None);
        inactive.Value.Data.ShouldHaveSingleItem().Name.ShouldBe("List 03");

        var search = await handler.Handle(new GetAllSupplyListsQuery { SearchTerm = "List 04" }, CancellationToken.None);
        search.Value.Data.ShouldHaveSingleItem().Name.ShouldBe("List 04");
    }
}

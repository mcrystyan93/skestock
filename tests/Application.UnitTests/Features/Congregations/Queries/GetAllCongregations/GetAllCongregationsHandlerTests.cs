using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;
using skestock.Application.Common.Filtering;
using skestock.Application.Common.Models;
using skestock.Application.Features.Congregations.Queries.GetAllCongregations;
using skestock.Domain.Entities.Congregations;

namespace skestock.Application.UnitTests.Features.Congregations.Queries.GetAllCongregations;

public class GetAllCongregationsHandlerTests
{
    private static readonly DateTimeOffset Baseline = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static CongregationTestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<CongregationTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new CongregationTestDbContext(options);
    }

    private static async Task<CongregationTestDbContext> SeedAsync(params Congregation[] congregations)
    {
        var context = CreateContext();
        var index = 0;
        foreach (var congregation in congregations)
        {
            congregation.Email = "c@example.com";
            congregation.CreatedDate = Baseline.AddMinutes(index);
            congregation.LastModifiedDate = congregation.CreatedDate;
            index++;
        }

        context.Congregations.AddRange(congregations);
        await context.SaveChangesAsync(CancellationToken.None);
        return context;
    }

    private static async Task<PaginatedResponse<skestock.Application.Features.Congregations.Models.CongregationDto>> RunAsync(
        CongregationTestDbContext context, GetAllCongregationsQuery query)
    {
        var result = await new GetAllCongregationsHandler(context).Handle(query, CancellationToken.None);
        result.IsSuccess.ShouldBeTrue();
        return result.Value;
    }

    [Test]
    public async Task Handle_ReturnsAllFieldsOfEachCongregation()
    {
        await using var context = await SeedAsync(new Congregation
        {
            Name = "Central",
            Coordinator = "Ana",
            CoordinatorPhone = "0711111111",
            DessertContactPerson = "Ion",
            DessertContactPhone = "0722222222",
            CanReceiveStudents = true,
            IsSmallCongregation = true,
            Message = "Hello"
        });

        var dto = (await RunAsync(context, new GetAllCongregationsQuery())).Data.Single();

        dto.Name.ShouldBe("Central");
        dto.Email.ShouldBe("c@example.com");
        dto.Coordinator.ShouldBe("Ana");
        dto.CoordinatorPhone.ShouldBe("0711111111");
        dto.DessertContactPerson.ShouldBe("Ion");
        dto.DessertContactPhone.ShouldBe("0722222222");
        dto.CanReceiveStudents.ShouldBeTrue();
        dto.IsSmallCongregation.ShouldBeTrue();
        dto.Message.ShouldBe("Hello");
    }

    [Test]
    public async Task Handle_DefaultSort_ReturnsMostRecentlyCreatedFirst()
    {
        await using var context = await SeedAsync(
            new Congregation { Name = "A" }, new Congregation { Name = "B" }, new Congregation { Name = "C" });

        var page = await RunAsync(context, new GetAllCongregationsQuery());

        page.Data.Select(c => c.Name).ShouldBe(["C", "B", "A"]);
    }

    [Test]
    public async Task Handle_SortByNameAscending_OrdersByName()
    {
        await using var context = await SeedAsync(
            new Congregation { Name = "B" }, new Congregation { Name = "C" }, new Congregation { Name = "A" });

        var page = await RunAsync(context, new GetAllCongregationsQuery
        {
            Sort = [new PaginationSort { Key = "name", Value = "ascend" }]
        });

        page.Data.Select(c => c.Name).ShouldBe(["A", "B", "C"]);
    }

    [Test]
    public async Task Handle_ExcludesSoftDeletedCongregations()
    {
        await using var context = await SeedAsync(
            new Congregation { Name = "Active" },
            new Congregation { Name = "Deleted", DeletedDate = Baseline });

        var page = await RunAsync(context, new GetAllCongregationsQuery());

        page.Data.Select(c => c.Name).ShouldBe(["Active"]);
    }

    [Test]
    public async Task Handle_WithSearchTerm_ReturnsOnlyMatchingNames()
    {
        await using var context = await SeedAsync(
            new Congregation { Name = "Central" }, new Congregation { Name = "Northside" });

        var page = await RunAsync(context, new GetAllCongregationsQuery { SearchTerm = "Centr" });

        page.Data.Select(c => c.Name).ShouldBe(["Central"]);
    }

    [Test]
    public async Task Handle_WithCanReceiveStudentsFilter_ReturnsOnlyMatching()
    {
        await using var context = await SeedAsync(
            new Congregation { Name = "Yes", CanReceiveStudents = true },
            new Congregation { Name = "No", CanReceiveStudents = false });

        var page = await RunAsync(context, new GetAllCongregationsQuery
        {
            Filters = [new ColumnFilter("canReceiveStudents", FilterOperator.Equals, true)]
        });

        page.Data.Select(c => c.Name).ShouldBe(["Yes"]);
    }

    [Test]
    public async Task Handle_WithIsSmallCongregationFilter_ReturnsOnlyMatching()
    {
        await using var context = await SeedAsync(
            new Congregation { Name = "Small", IsSmallCongregation = true },
            new Congregation { Name = "Big", IsSmallCongregation = false });

        var page = await RunAsync(context, new GetAllCongregationsQuery
        {
            Filters = [new ColumnFilter("isSmallCongregation", FilterOperator.Equals, false)]
        });

        page.Data.Select(c => c.Name).ShouldBe(["Big"]);
    }

    [Test]
    public async Task Handle_WithPageSizeSmallerThanTotal_PagesThroughAllRowsWithoutDuplicates()
    {
        await using var context = await SeedAsync(
            new Congregation { Name = "A" }, new Congregation { Name = "B" }, new Congregation { Name = "C" },
            new Congregation { Name = "D" }, new Congregation { Name = "E" });

        var first = await RunAsync(context, new GetAllCongregationsQuery { PageSize = 2 });
        first.HasNextPage.ShouldBeTrue();
        first.Data.Select(c => c.Name).ShouldBe(["E", "D"]);

        var second = await RunAsync(context, new GetAllCongregationsQuery { PageSize = 2, Cursor = first.NextCursor });
        second.HasNextPage.ShouldBeTrue();
        second.Data.Select(c => c.Name).ShouldBe(["C", "B"]);

        var third = await RunAsync(context, new GetAllCongregationsQuery { PageSize = 2, Cursor = second.NextCursor });
        third.HasNextPage.ShouldBeFalse();
        third.Data.Select(c => c.Name).ShouldBe(["A"]);
    }

    [Test]
    public void Query_UsesCongregationListTagAndDistinctCacheKeysPerFilter()
    {
        var all = new GetAllCongregationsQuery();
        var filtered = new GetAllCongregationsQuery
        {
            Filters = [new ColumnFilter("canReceiveStudents", FilterOperator.Equals, true)]
        };

        all.Tags.ShouldBe([skestock.Application.Features.Congregations.CacheConstants.CongregationListTag]);
        all.BuildCacheKey().ShouldNotBe(filtered.BuildCacheKey());
    }
}

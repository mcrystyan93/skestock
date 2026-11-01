using skestock.Application.Common.Filtering;
using skestock.Application.Features.Congregations.Commands.CreateCongregation;
using skestock.Application.Features.Congregations.Queries.GetAllCongregations;
using skestock.Domain.Entities.Users;

namespace skestock.Application.FunctionalTests.Features.Congregations;

public class GetAllCongregationsQueryTests : TestBase
{
    private string _prefix = null!;

    [SetUp]
    public void SetUpPrefix()
    {
        _prefix = $"FT{Guid.NewGuid():N}"[..10];
    }

    private Task<FluentResults.Result<skestock.Application.Features.Congregations.Models.CongregationMutationDto>> CreateAsync(
        string name, bool canReceiveStudents = false, bool isSmall = false) =>
        TestApp.SendAsync(new CreateCongregationCommand
        {
            Name = $"{_prefix}-{name}",
            Email = "c@example.com",
            CanReceiveStudents = canReceiveStudents,
            IsSmallCongregation = isSmall
        });

    [Test]
    public async Task CanReceiveStudentsFilter_ReturnsOnlyMatching()
    {
        await CreateAsync("Yes", canReceiveStudents: true);
        await CreateAsync("No");

        var result = await TestApp.SendAsync(new GetAllCongregationsQuery
        {
            SearchTerm = _prefix,
            Filters = [new ColumnFilter("canReceiveStudents", FilterOperator.Equals, true)]
        });

        result.Value.Data.Select(c => c.Name).ShouldBe([$"{_prefix}-Yes"]);
    }

    [Test]
    public async Task IsSmallCongregationFilter_ReturnsOnlyMatching()
    {
        await CreateAsync("Small", isSmall: true);
        await CreateAsync("Big");

        var result = await TestApp.SendAsync(new GetAllCongregationsQuery
        {
            SearchTerm = _prefix,
            Filters = [new ColumnFilter("isSmallCongregation", FilterOperator.Equals, true)]
        });

        result.Value.Data.Select(c => c.Name).ShouldBe([$"{_prefix}-Small"]);
    }

    [Test]
    public async Task Paging_WalksAllRowsWithoutDuplicates()
    {
        foreach (var n in new[] { "A", "B", "C", "D", "E" })
            await CreateAsync(n);

        var seen = new List<string>();
        string? cursor = null;
        bool hasNext;
        do
        {
            var page = (await TestApp.SendAsync(new GetAllCongregationsQuery
            {
                SearchTerm = _prefix,
                PageSize = 2,
                Cursor = cursor
            })).Value;
            seen.AddRange(page.Data.Select(c => c.Name));
            cursor = page.NextCursor;
            hasNext = page.HasNextPage;
        } while (hasNext);

        seen.Count.ShouldBe(5);
        seen.Distinct().Count().ShouldBe(5);
    }

    [Test]
    public async Task SearchIsAccentInsensitive()
    {
        await CreateAsync("Café");

        var result = await TestApp.SendAsync(new GetAllCongregationsQuery { SearchTerm = $"{_prefix}-Cafe" });

        result.Value.Data.Count().ShouldBe(1);
    }

    [Test]
    public async Task CreatedByName_IsPopulatedFromTheAuditUser()
    {
        // Audit columns are FKs to UserProfile.IdentityId, so the Identity user needs a matching profile.
        var userId = await TestApp.RunAsDefaultUserAsync();
        await TestApp.AddAsync(new UserProfile { IdentityId = userId!.Value, FirstName = "Staff", LastName = "Member" });
        await CreateAsync("Audited");

        var dto = (await TestApp.SendAsync(new GetAllCongregationsQuery { SearchTerm = _prefix })).Value.Data.Single();

        dto.CreatedByName.ShouldNotBeNullOrWhiteSpace();
    }
}

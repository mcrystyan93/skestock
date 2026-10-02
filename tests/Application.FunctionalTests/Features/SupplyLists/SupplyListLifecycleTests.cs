using skestock.Application.Common.Errors;
using skestock.Application.Common.Exceptions;
using skestock.Application.Common.Filtering;
using skestock.Application.Features.SupplyLists.Commands.CreateSupplyList;
using skestock.Application.Features.SupplyLists.Commands.DisableSupplyList;
using skestock.Application.Features.SupplyLists.Commands.EnableSupplyList;
using skestock.Application.Features.SupplyLists.Commands.UpdateSupplyList;
using skestock.Application.Features.SupplyLists.Models;
using skestock.Application.Features.SupplyLists.Queries.GetAllSupplyLists;
using skestock.Application.Features.SupplyLists.Queries.GetSupplyListById;
using skestock.Domain.Entities;
using skestock.Domain.Entities.Categories;
using skestock.Domain.Entities.Items;
using skestock.Domain.Entities.Users;
using skestock.Domain.Enums;

namespace skestock.Application.FunctionalTests.Features.SupplyLists;

public class SupplyListLifecycleTests : TestBase
{
    private string _prefix = null!;
    private Item _item = null!;

    [SetUp]
    public async Task SetUpPrerequisites()
    {
        _prefix = $"FT{Guid.NewGuid():N}"[..10];

        var category = new Category { Name = $"{_prefix}-Category" };
        await TestApp.AddAsync(category);

        _item = new Item { Name = $"{_prefix}-Rice", Unit = "kg", CategoryId = category.Id };
        await TestApp.AddAsync(_item);

        var userId = await TestApp.RunAsDefaultUserAsync();
        await TestApp.AddAsync(new UserProfile { IdentityId = userId!.Value, FirstName = "Staff", LastName = "Member" });
    }

    [Test]
    public async Task CreateUpdateDisableEnable_FollowsFullLifecycle()
    {
        var created = await TestApp.SendAsync(new CreateSupplyListCommand
        {
            Name = $"{_prefix}-Weekly",
            Frequency = SupplyListFrequency.Weekly,
            Lines = [new SupplyListLineInput { ItemId = _item.Id }]
        });

        created.IsSuccess.ShouldBeTrue();
        created.Value.IsActive.ShouldBeTrue();
        created.Value.CreatedByName.ShouldBe("Staff Member");
        var line = created.Value.Lines.ShouldHaveSingleItem();
        line.Quantity.ShouldBe(1);
        line.Unit.ShouldBe("kg");

        var id = created.Value.Id;

        var updated = await TestApp.SendAsync(new UpdateSupplyListCommand
        {
            Id = id,
            Name = $"{_prefix}-Biweekly",
            Frequency = SupplyListFrequency.EveryXWeeks,
            IntervalWeeks = 2,
            Lines = [new SupplyListLineInput { ItemId = _item.Id, Quantity = 4, Unit = "g", Notes = "fine" }]
        });

        updated.IsSuccess.ShouldBeTrue();
        updated.Value.IntervalWeeks.ShouldBe(2);
        updated.Value.Lines.ShouldHaveSingleItem().Quantity.ShouldBe(4);

        (await TestApp.SendAsync(new DisableSupplyListCommand { Id = id })).Value.IsActive.ShouldBeFalse();

        var blocked = await TestApp.SendAsync(new UpdateSupplyListCommand
        {
            Id = id, Name = $"{_prefix}-Biweekly", Frequency = SupplyListFrequency.Once
        });
        blocked.Errors.ShouldContain(e => e is SupplyListErrors.SupplyListNotEditable);

        (await TestApp.SendAsync(new EnableSupplyListCommand { Id = id })).Value.IsActive.ShouldBeTrue();

        var fetched = await TestApp.SendAsync(new GetSupplyListByIdQuery { Id = id });
        fetched.Value.Name.ShouldBe($"{_prefix}-Biweekly");
        fetched.Value.Lines.ShouldHaveSingleItem().Notes.ShouldBe("fine");
    }

    [Test]
    public async Task Create_RejectsDuplicateName()
    {
        var command = new CreateSupplyListCommand { Name = $"{_prefix}-Dup", Frequency = SupplyListFrequency.Once };
        (await TestApp.SendAsync(command)).IsSuccess.ShouldBeTrue();

        await Should.ThrowAsync<ValidationException>(() => TestApp.SendAsync(new CreateSupplyListCommand { Name = $"{_prefix}-DUP", Frequency = SupplyListFrequency.Once }));
    }

    [Test]
    public async Task GetAll_PaginatesAndFilters()
    {
        var ids = new List<Guid>();
        foreach (var n in new[] { "A", "B", "C" })
        {
            var created = await TestApp.SendAsync(new CreateSupplyListCommand
            {
                Name = $"{_prefix}-{n}",
                Frequency = SupplyListFrequency.Monthly,
                Lines = [new SupplyListLineInput { ItemId = _item.Id }]
            });
            ids.Add(created.Value.Id);
        }
        await TestApp.SendAsync(new DisableSupplyListCommand { Id = ids[2] });

        var first = await TestApp.SendAsync(new GetAllSupplyListsQuery { PageSize = 2, SearchTerm = _prefix });
        first.Value.Data.Select(d => d.Name).ShouldBe([$"{_prefix}-A", $"{_prefix}-B"]);
        first.Value.HasNextPage.ShouldBeTrue();

        var second = await TestApp.SendAsync(
            new GetAllSupplyListsQuery { PageSize = 2, SearchTerm = _prefix, Cursor = first.Value.NextCursor });
        second.Value.Data.ShouldHaveSingleItem().Name.ShouldBe($"{_prefix}-C");

        var inactive = await TestApp.SendAsync(new GetAllSupplyListsQuery
        {
            SearchTerm = _prefix,
            Filters = [new ColumnFilter("isActive", FilterOperator.Equals, false)]
        });
        inactive.Value.Data.ShouldHaveSingleItem().Id.ShouldBe(ids[2]);
    }
}

using skestock.Application.Common.Errors;
using skestock.Application.Features.OrderLists.Commands.CancelOrderList;
using skestock.Application.Features.OrderLists.Commands.CreateOrderList;
using skestock.Application.Features.OrderLists.Commands.DeleteOrderList;
using skestock.Application.Features.OrderLists.Commands.SubmitOrderList;
using skestock.Application.Features.OrderLists.Commands.UpdateOrderList;
using skestock.Application.Features.OrderLists.Models;
using skestock.Application.Features.OrderLists.Queries.GetOrderListById;
using skestock.Domain.Entities;
using skestock.Domain.Enums;

namespace skestock.Application.FunctionalTests.Features.OrderLists.Commands;

public class OrderListLifecycleTests : TestBase
{
    private string _prefix = null!;

    [SetUp]
    public void SetUpPrefix()
    {
        _prefix = $"FT{Guid.NewGuid():N}"[..10];
    }

    private async Task<(SchoolClass Class, Item Item)> SeedPrerequisitesAsync()
    {
        var category = new Category { Name = $"{_prefix}-Category" };
        await TestApp.AddAsync(category);

        var item = new Item { Name = $"{_prefix}-Rice", Unit = "kg", CategoryId = category.Id };
        await TestApp.AddAsync(item);

        var schoolClass = new SchoolClass
        {
            Name = $"{_prefix}-Class",
            StartDate = new DateOnly(2024, 1, 1),
            EndDate = new DateOnly(2024, 6, 1)
        };
        await TestApp.AddAsync(schoolClass);

        return (schoolClass, item);
    }

    private static async Task RunAsUserWithProfileAsync()
    {
        var userId = await TestApp.RunAsDefaultUserAsync();
        await TestApp.AddAsync(new UserProfile { IdentityId = userId!.Value, FirstName = "Staff", LastName = "Member" });
    }

    [Test]
    public async Task CreateUpdateSubmit_FollowsFullLifecycle()
    {
        var (schoolClass, item) = await SeedPrerequisitesAsync();
        await RunAsUserWithProfileAsync();

        var created = await TestApp.SendAsync(new CreateOrderListCommand
        {
            ClassId = schoolClass.Id,
            Name = $"{_prefix}-Weekly",
            Note = "First order",
            Lines =
            [
                new OrderListLineInput { ItemId = item.Id, Quantity = 5 },
                new OrderListLineInput { ProductName = "Handmade widget", Quantity = 3, Notes = "not in catalog" }
            ]
        });

        created.IsSuccess.ShouldBeTrue();
        created.Value.Status.ShouldBe(OrderListStatus.Draft.ToString());
        created.Value.ClassName.ShouldBe(schoolClass.Name);
        created.Value.Lines.Count.ShouldBe(2);
        created.Value.Lines.ShouldContain(l => l.ItemId == item.Id && l.ProductName == item.Name);
        created.Value.Lines.ShouldContain(l => l.ItemId == null && l.ProductName == "Handmade widget");

        var id = created.Value.Id;

        var updated = await TestApp.SendAsync(new UpdateOrderListCommand
        {
            Id = id,
            Name = $"{_prefix}-Renamed",
            Note = "Revised",
            Lines = [new OrderListLineInput { ItemId = item.Id, Quantity = 10 }]
        });

        updated.IsSuccess.ShouldBeTrue();
        updated.Value.Name.ShouldBe($"{_prefix}-Renamed");
        updated.Value.Lines.Count.ShouldBe(1);
        updated.Value.Lines.Single().Quantity.ShouldBe(10);

        var submitted = await TestApp.SendAsync(new SubmitOrderListCommand { Id = id });

        submitted.IsSuccess.ShouldBeTrue();
        submitted.Value.Status.ShouldBe(OrderListStatus.Submitted.ToString());
        submitted.Value.SubmittedAt.ShouldNotBeNull();

        var fetched = await TestApp.SendAsync(new GetOrderListByIdQuery { Id = id });
        fetched.IsSuccess.ShouldBeTrue();
        fetched.Value.Status.ShouldBe(OrderListStatus.Submitted.ToString());
    }

    [Test]
    public async Task Update_AfterSubmit_FailsBecauseNotEditable()
    {
        var (schoolClass, item) = await SeedPrerequisitesAsync();
        await RunAsUserWithProfileAsync();

        var created = await TestApp.SendAsync(new CreateOrderListCommand
        {
            ClassId = schoolClass.Id,
            Name = $"{_prefix}-List",
            Lines = [new OrderListLineInput { ItemId = item.Id, Quantity = 1 }]
        });
        await TestApp.SendAsync(new SubmitOrderListCommand { Id = created.Value.Id });

        var result = await TestApp.SendAsync(new UpdateOrderListCommand
        {
            Id = created.Value.Id,
            Lines = [new OrderListLineInput { ItemId = item.Id, Quantity = 2 }]
        });

        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(e => e is OrderListErrors.OrderListNotEditable);
    }

    [Test]
    public async Task Submit_WithEmptyList_Fails()
    {
        var (schoolClass, _) = await SeedPrerequisitesAsync();
        await RunAsUserWithProfileAsync();

        var created = await TestApp.SendAsync(new CreateOrderListCommand
        {
            ClassId = schoolClass.Id,
            Name = $"{_prefix}-Empty"
        });

        var result = await TestApp.SendAsync(new SubmitOrderListCommand { Id = created.Value.Id });

        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(e => e is OrderListErrors.OrderListEmpty);
    }

    [Test]
    public async Task Cancel_ThenDelete_Succeeds()
    {
        var (schoolClass, item) = await SeedPrerequisitesAsync();
        await RunAsUserWithProfileAsync();

        var created = await TestApp.SendAsync(new CreateOrderListCommand
        {
            ClassId = schoolClass.Id,
            Name = $"{_prefix}-Cancelable",
            Lines = [new OrderListLineInput { ItemId = item.Id, Quantity = 1 }]
        });
        await TestApp.SendAsync(new SubmitOrderListCommand { Id = created.Value.Id });

        var cancelled = await TestApp.SendAsync(new CancelOrderListCommand { Id = created.Value.Id });
        cancelled.IsSuccess.ShouldBeTrue();
        cancelled.Value.Status.ShouldBe(OrderListStatus.Cancelled.ToString());

        var deleted = await TestApp.SendAsync(new DeleteOrderListCommand { Id = created.Value.Id });
        deleted.IsSuccess.ShouldBeTrue();

        var fetched = await TestApp.SendAsync(new GetOrderListByIdQuery { Id = created.Value.Id });
        fetched.IsFailed.ShouldBeTrue();
    }

    [Test]
    public async Task Delete_WhileSubmitted_Fails()
    {
        var (schoolClass, item) = await SeedPrerequisitesAsync();
        await RunAsUserWithProfileAsync();

        var created = await TestApp.SendAsync(new CreateOrderListCommand
        {
            ClassId = schoolClass.Id,
            Name = $"{_prefix}-Locked",
            Lines = [new OrderListLineInput { ItemId = item.Id, Quantity = 1 }]
        });
        await TestApp.SendAsync(new SubmitOrderListCommand { Id = created.Value.Id });

        var result = await TestApp.SendAsync(new DeleteOrderListCommand { Id = created.Value.Id });

        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(e => e is OrderListErrors.OrderListNotDeletable);
    }
}

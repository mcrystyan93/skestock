using ClosedXML.Excel;
using skestock.Application.Common.Errors;
using skestock.Application.Features.OrderLists.Commands.CreateOrderList;
using skestock.Application.Features.OrderLists.Commands.SubmitOrderList;
using skestock.Application.Features.OrderLists.Models;
using skestock.Application.Features.OrderLists.Queries.ExportOrderList;
using skestock.Domain.Entities;

namespace skestock.Application.FunctionalTests.Features.OrderLists.Queries;

public class ExportOrderListQueryTests : TestBase
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
    public async Task Export_SubmittedList_ReturnsWorkbook()
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
                new OrderListLineInput { ItemId = item.Id, Quantity = 5, Unit = "kg" },
                new OrderListLineInput { ProductName = "Handmade widget", Quantity = 3, Unit = "buc" }
            ]
        });
        await TestApp.SendAsync(new SubmitOrderListCommand { Id = created.Value.Id });

        var result = await TestApp.SendAsync(new ExportOrderListQuery { Id = created.Value.Id });

        result.IsSuccess.ShouldBeTrue();
        result.Value.FileName.ShouldBe($"{_prefix}-Weekly.xlsx");
        result.Value.ContentType.ShouldBe("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        result.Value.Content.ShouldNotBeEmpty();

        using var stream = new MemoryStream(result.Value.Content);
        using var workbook = new XLWorkbook(stream);
        var cells = workbook.Worksheet("Comanda").CellsUsed().Select(c => c.GetString()).ToList();

        cells.ShouldContain($"{_prefix}-Category");
        cells.ShouldContain("Alte articole");
        cells.ShouldContain(item.Name);
        cells.ShouldContain("Handmade widget");
    }

    [Test]
    public async Task Export_DraftList_FailsBecauseNotExportable()
    {
        var (schoolClass, item) = await SeedPrerequisitesAsync();
        await RunAsUserWithProfileAsync();

        var created = await TestApp.SendAsync(new CreateOrderListCommand
        {
            ClassId = schoolClass.Id,
            Name = $"{_prefix}-Draft",
            Lines = [new OrderListLineInput { ItemId = item.Id, Quantity = 1, Unit = "kg" }]
        });

        var result = await TestApp.SendAsync(new ExportOrderListQuery { Id = created.Value.Id });

        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(e => e is OrderListErrors.OrderListNotExportable);
    }
}

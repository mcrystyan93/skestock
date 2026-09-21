using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;
using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.OrderLists.Models;
using skestock.Application.Features.OrderLists.Queries.ExportOrderList;
using skestock.Domain.Entities;

namespace skestock.Application.UnitTests.Features.OrderLists.Queries.ExportOrderList;

public class ExportOrderListHandlerTests
{
    private sealed class RecordingExporter : IOrderListExcelExporter
    {
        public OrderListExportModel? Model { get; private set; }

        public byte[] Export(OrderListExportModel model)
        {
            Model = model;
            return [1, 2, 3];
        }
    }

    private static async Task<(OrderListTestDbContext Context, SchoolClass Class, Category Vegetables, Category Bakery)>
        CreateContextAsync()
    {
        var options = new DbContextOptionsBuilder<OrderListTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new OrderListTestDbContext(options);

        var schoolClass = new SchoolClass
        {
            Name = "Fall 2026",
            StartDate = new DateOnly(2026, 9, 1),
            EndDate = new DateOnly(2026, 12, 20)
        };
        context.SchoolClasses.Add(schoolClass);

        var vegetables = new Category { Name = "Legume" };
        var bakery = new Category { Name = "Brutărie" };
        context.Categories.AddRange(vegetables, bakery);
        await context.SaveChangesAsync(CancellationToken.None);

        return (context, schoolClass, vegetables, bakery);
    }

    private static Item AddItem(OrderListTestDbContext context, Category category, string name)
    {
        var item = new Item { Name = name, CategoryId = category.Id };
        context.Items.Add(item);
        return item;
    }

    [Test]
    public async Task ShouldGroupByCategoryWithAlteArticoleLastAndSortedLines()
    {
        var (context, schoolClass, vegetables, bakery) = await CreateContextAsync();
        await using var _ = context;

        var carrot = AddItem(context, vegetables, "Morcovi");
        var bread = AddItem(context, bakery, "Pâine");
        await context.SaveChangesAsync(CancellationToken.None);

        var orderList = OrderList.Create(schoolClass.Id, "Comanda Test", "O notă");
        orderList.Lines.Add(new OrderListLine { ItemId = bread.Id, ProductName = "Pâine", Quantity = 3 });
        orderList.Lines.Add(new OrderListLine { ItemId = carrot.Id, ProductName = "Morcovi", Quantity = 5 });
        orderList.Lines.Add(new OrderListLine { ProductName = "Șervețele", Quantity = 2 });
        orderList.Lines.Add(new OrderListLine { ProductName = "Ambalaje", Quantity = 1 });
        orderList.Submit();
        context.OrderLists.Add(orderList);
        await context.SaveChangesAsync(CancellationToken.None);

        var exporter = new RecordingExporter();
        var handler = new ExportOrderListHandler(context, exporter);

        var result = await handler.Handle(new ExportOrderListQuery { Id = orderList.Id }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        exporter.Model.ShouldNotBeNull();

        var groups = exporter.Model!.Groups;
        groups.Select(g => g.CategoryName).ShouldBe(["Brutărie", "Legume", "Alte articole"]);

        var alteArticole = groups.Single(g => g.CategoryName == "Alte articole");
        alteArticole.Lines.Select(l => l.ProductName).ShouldBe(["Ambalaje", "Șervețele"]);
    }

    [Test]
    public async Task ShouldUseListNameAsFileName()
    {
        var (context, schoolClass, vegetables, _) = await CreateContextAsync();
        await using var _ = context;

        var carrot = AddItem(context, vegetables, "Morcovi");
        await context.SaveChangesAsync(CancellationToken.None);

        var orderList = OrderList.Create(schoolClass.Id, "Comanda Mea", null);
        orderList.Lines.Add(new OrderListLine { ItemId = carrot.Id, ProductName = "Morcovi", Quantity = 5 });
        orderList.Submit();
        context.OrderLists.Add(orderList);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new ExportOrderListHandler(context, new RecordingExporter());

        var result = await handler.Handle(new ExportOrderListQuery { Id = orderList.Id }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.FileName.ShouldBe("Comanda Mea.xlsx");
        result.Value.ContentType.ShouldBe("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
    }

    [Test]
    public async Task ShouldFallBackToClassNameWhenNameMissing()
    {
        var (context, schoolClass, vegetables, _) = await CreateContextAsync();
        await using var _ = context;

        var carrot = AddItem(context, vegetables, "Morcovi");
        await context.SaveChangesAsync(CancellationToken.None);

        var orderList = OrderList.Create(schoolClass.Id, null, null);
        orderList.Lines.Add(new OrderListLine { ItemId = carrot.Id, ProductName = "Morcovi", Quantity = 5 });
        orderList.Submit();
        context.OrderLists.Add(orderList);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new ExportOrderListHandler(context, new RecordingExporter());

        var result = await handler.Handle(new ExportOrderListQuery { Id = orderList.Id }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.FileName.ShouldStartWith("Comanda-Fall 2026-");
        result.Value.FileName.ShouldEndWith(".xlsx");
    }

    [Test]
    public async Task ShouldFailWhenNotSubmitted()
    {
        var (context, schoolClass, vegetables, _) = await CreateContextAsync();
        await using var _ = context;

        var carrot = AddItem(context, vegetables, "Morcovi");
        await context.SaveChangesAsync(CancellationToken.None);

        var orderList = OrderList.Create(schoolClass.Id, "Comanda", null);
        orderList.Lines.Add(new OrderListLine { ItemId = carrot.Id, ProductName = "Morcovi", Quantity = 5 });
        context.OrderLists.Add(orderList);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new ExportOrderListHandler(context, new RecordingExporter());

        var result = await handler.Handle(new ExportOrderListQuery { Id = orderList.Id }, CancellationToken.None);

        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(e => e is OrderListErrors.OrderListNotExportable);
    }

    [Test]
    public async Task ShouldFailWhenNotFound()
    {
        var (context, _, _, _) = await CreateContextAsync();
        await using var _ = context;

        var handler = new ExportOrderListHandler(context, new RecordingExporter());

        var result = await handler.Handle(new ExportOrderListQuery { Id = Guid.NewGuid() }, CancellationToken.None);

        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(e => e is OrderListErrors.OrderListNotFound);
    }
}

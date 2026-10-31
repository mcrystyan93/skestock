using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;
using skestock.Application.Features.OrderLists.Queries.GetOrderListById;
using skestock.Domain.Entities;
using skestock.Domain.Entities.Categories;
using skestock.Domain.Entities.Items;
using skestock.Domain.Entities.OrderLists;
using skestock.Domain.Entities.SchoolClasses;

namespace skestock.Application.UnitTests.Features.OrderLists.Queries.GetOrderListById;

public class GetOrderListByIdHandlerTests
{
    [Test]
    public async Task ShouldExposeItemImageFileIdOnCatalogLinesOnly()
    {
        var options = new DbContextOptionsBuilder<OrderListTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new OrderListTestDbContext(options);

        var schoolClass = new SchoolClass
        {
            Name = "Fall 2026",
            StartDate = new DateOnly(2026, 9, 1),
            EndDate = new DateOnly(2026, 12, 20)
        };
        var category = new Category { Name = "Stationery" };
        context.SchoolClasses.Add(schoolClass);
        context.Categories.Add(category);
        await context.SaveChangesAsync(CancellationToken.None);

        var imageFileId = Guid.NewGuid();
        var item = new Item { Name = "Pencil", Unit = "unit", CategoryId = category.Id, ImageFileId = imageFileId };
        context.Items.Add(item);
        await context.SaveChangesAsync(CancellationToken.None);

        var orderList = OrderList.Create(schoolClass.Id, "Order", null);
        orderList.Lines.Add(new OrderListLine { ItemId = item.Id, ProductName = "Pencil", Quantity = 1 });
        orderList.Lines.Add(new OrderListLine { ProductName = "Napkins", Quantity = 2 });
        context.OrderLists.Add(orderList);
        await context.SaveChangesAsync(CancellationToken.None);

        var result = await new GetOrderListByIdHandler(context)
            .Handle(new GetOrderListByIdQuery { Id = orderList.Id }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Lines.Single(l => l.ItemId == item.Id).ItemImageFileId.ShouldBe(imageFileId);
        result.Value.Lines.Single(l => l.ItemId == null).ItemImageFileId.ShouldBeNull();
    }
}

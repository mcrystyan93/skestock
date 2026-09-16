using Microsoft.EntityFrameworkCore;
using skestock.Application.Common.Interfaces;
using skestock.Application.UnitTests.Features.GoodsReceipts.Commands.CreateGoodsReceipt;
using skestock.Domain.Entities;

namespace skestock.Application.UnitTests.Features.Stock.Commands.MoveStock;

internal static class MoveStockTestData
{
    public static async Task<MoveStockFixture> CreateFixtureAsync()
    {
        var options = new DbContextOptionsBuilder<GoodsReceiptTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new GoodsReceiptTestDbContext(options);
        var category = new Category { Name = "Pantry" };
        var item = new Item
        {
            Name = "Rice",
            Unit = "kg",
            MinThreshold = 10,
            IsPerishable = true,
            Category = category
        };
        var sourceLocation = new Location { Name = "Main Storage", Type = "StorageRoom" };
        var destinationLocation = new Location { Name = "Classroom", Type = "Classroom" };
        var schoolClass = new SchoolClass
        {
            Name = "Fall 2026", StartDate = new DateOnly(2026, 9, 1), EndDate = new DateOnly(2026, 12, 20)
        };
        var userProfile = new UserProfile { IdentityId = Guid.NewGuid(), FirstName = "Staff", LastName = "Member" };

        context.Categories.Add(category);
        context.Items.Add(item);
        context.Locations.AddRange(sourceLocation, destinationLocation);
        context.SchoolClasses.Add(schoolClass);
        context.UserProfiles.Add(userProfile);
        await context.SaveChangesAsync(CancellationToken.None);

        return new MoveStockFixture(
            context,
            item,
            sourceLocation,
            destinationLocation,
            schoolClass,
            userProfile);
    }

    public static StockBatch CreateBatch(
        MoveStockFixture fixture,
        int quantity,
        DateOnly? expiryDate,
        DateOnly receivedDate,
        decimal unitPrice,
        Guid? goodsReceiptId = null,
        Guid? classId = null)
    {
        return new StockBatch
        {
            ItemId = fixture.Item.Id,
            LocationId = fixture.SourceLocation.Id,
            ReceivedClassId = classId ?? fixture.Class.Id,
            Quantity = quantity,
            ExpiryDate = expiryDate,
            ReceivedDate = receivedDate,
            UnitPrice = unitPrice,
            GoodsReceiptId = goodsReceiptId
        };
    }
}

internal sealed record MoveStockFixture(
    GoodsReceiptTestDbContext Context,
    Item Item,
    Location SourceLocation,
    Location DestinationLocation,
    SchoolClass Class,
    UserProfile UserProfile);

internal sealed class MoveStockFakeUser(Guid? id) : IUser
{
    public Guid? Id { get; } = id;
    public List<string>? Roles { get; } = [];
}

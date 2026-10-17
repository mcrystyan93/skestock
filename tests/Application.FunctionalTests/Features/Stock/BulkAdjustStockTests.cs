using skestock.Application.Common.Exceptions;
using skestock.Application.Features.Stock.Commands.BulkAdjustStock;
using skestock.Domain.Entities.Categories;
using skestock.Domain.Entities.Items;
using skestock.Domain.Entities.Locations;
using skestock.Domain.Entities.SchoolClasses;
using skestock.Domain.Entities.Stock;
using skestock.Domain.Entities.Users;
using skestock.Domain.Enums;

namespace skestock.Application.FunctionalTests.Features.Stock;

public class BulkAdjustStockTests : TestBase
{
    private static async Task<(SchoolClass Class, Item Rice, Item Flour, Location Location, StockBatch RiceBatch, StockBatch FlourBatch)> SeedAsync()
    {
        var userId = await TestApp.RunAsAdministratorAsync();
        await TestApp.AddAsync(new UserProfile { IdentityId = userId!.Value, FirstName = "Staff", LastName = "Member" });

        var category = new Category { Name = $"FT{Guid.NewGuid():N}"[..10] };
        await TestApp.AddAsync(category);
        var rice = new Item { Name = "Rice", Unit = "kg", MinThreshold = 5, CategoryId = category.Id };
        var flour = new Item { Name = "Flour", Unit = "kg", MinThreshold = 5, CategoryId = category.Id };
        await TestApp.AddAsync(rice);
        await TestApp.AddAsync(flour);
        var location = new Location { Name = "Storage", Type = "StorageRoom" };
        await TestApp.AddAsync(location);
        var schoolClass = new SchoolClass
        {
            Name = $"cls-{Guid.NewGuid():N}"[..20],
            StartDate = new DateOnly(2026, 9, 1),
            EndDate = new DateOnly(2026, 12, 20)
        };
        await TestApp.AddAsync(schoolClass);

        StockBatch Batch(Item item, int qty) => new()
        {
            ItemId = item.Id, LocationId = location.Id, ReceivedClassId = schoolClass.Id,
            Quantity = qty, UnitPrice = 1m, ReceivedDate = new DateOnly(2026, 1, 1)
        };
        var riceBatch = Batch(rice, 10);
        var flourBatch = Batch(flour, 10);
        await TestApp.AddAsync(riceBatch);
        await TestApp.AddAsync(flourBatch);
        return (schoolClass, rice, flour, location, riceBatch, flourBatch);
    }

    [Test]
    public async Task ValidLines_AreAllPersisted()
    {
        var s = await SeedAsync();

        var result = await TestApp.SendAsync(new BulkAdjustStockCommand
        {
            ClassId = s.Class.Id,
            Reason = AdjustmentReason.Miscount,
            Lines = [new(s.Rice.Id, s.Location.Id, 4), new(s.Flour.Id, s.Location.Id, 13)]
        });

        result.IsSuccess.ShouldBeTrue();
        var rice = await TestApp.FindAsync<StockBatch>(s.RiceBatch.Id);
        rice!.Quantity.ShouldBe(4);
        // 2 seeded batches + 1 surplus batch for the flour increase
        (await TestApp.CountAsync<StockBatch>()).ShouldBe(3);
    }

    [Test]
    public async Task UnknownItem_PersistsNothing()
    {
        var s = await SeedAsync();

        await Should.ThrowAsync<ValidationException>(() => TestApp.SendAsync(new BulkAdjustStockCommand
        {
            ClassId = s.Class.Id,
            Reason = AdjustmentReason.Miscount,
            Lines = [new(s.Rice.Id, s.Location.Id, 4), new(Guid.NewGuid(), s.Location.Id, 1)]
        }));

        var rice = await TestApp.FindAsync<StockBatch>(s.RiceBatch.Id);
        rice!.Quantity.ShouldBe(10);
    }
}

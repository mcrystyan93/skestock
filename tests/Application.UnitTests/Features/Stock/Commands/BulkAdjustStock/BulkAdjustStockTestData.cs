using Microsoft.EntityFrameworkCore;
using skestock.Application.UnitTests.Features.GoodsReceipts.Commands.CreateGoodsReceipt;
using skestock.Domain.Entities.Categories;
using skestock.Domain.Entities.Items;
using skestock.Domain.Entities.Locations;
using skestock.Domain.Entities.SchoolClasses;
using skestock.Domain.Entities.Stock;
using skestock.Domain.Entities.Users;

namespace skestock.Application.UnitTests.Features.Stock.Commands.BulkAdjustStock;

internal sealed class BulkAdjustStockTestData
{
    public required GoodsReceiptTestDbContext Context { get; init; }
    public required Item Rice { get; init; }
    public required Item Flour { get; init; }
    public required Location Pantry { get; init; }
    public required Location Cellar { get; init; }
    public required SchoolClass Class { get; init; }
    public required UserProfile User { get; init; }

    public static async Task<BulkAdjustStockTestData> CreateAsync(Func<DbContextOptions<GoodsReceiptTestDbContext>, GoodsReceiptTestDbContext>? factory = null)
    {
        var options = new DbContextOptionsBuilder<GoodsReceiptTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = factory?.Invoke(options) ?? new GoodsReceiptTestDbContext(options);

        var category = new Category { Name = "Pantry" };
        var data = new BulkAdjustStockTestData
        {
            Context = context,
            Rice = new Item { Name = "Rice", Unit = "kg", MinThreshold = 10, IsPerishable = false, Category = category },
            Flour = new Item { Name = "Flour", Unit = "kg", MinThreshold = 10, IsPerishable = false, Category = category },
            Pantry = new Location { Name = "Pantry", Type = "StorageRoom" },
            Cellar = new Location { Name = "Cellar", Type = "StorageRoom" },
            Class = new SchoolClass
            {
                Name = "Fall 2026",
                StartDate = new DateOnly(2026, 9, 1),
                EndDate = new DateOnly(2026, 12, 20)
            },
            User = new UserProfile { IdentityId = Guid.NewGuid(), FirstName = "Staff", LastName = "Member" }
        };
        context.Categories.Add(category);
        context.Items.AddRange(data.Rice, data.Flour);
        context.Locations.AddRange(data.Pantry, data.Cellar);
        context.SchoolClasses.Add(data.Class);
        context.UserProfiles.Add(data.User);
        await context.SaveChangesAsync(CancellationToken.None);
        return data;
    }

    public StockBatch AddBatch(Item item, Location location, int quantity, DateOnly? expiry = null)
    {
        var batch = new StockBatch
        {
            ItemId = item.Id,
            LocationId = location.Id,
            ReceivedClassId = Class.Id,
            Quantity = quantity,
            ExpiryDate = expiry,
            ReceivedDate = new DateOnly(2026, 1, 1),
            UnitPrice = 2.5m
        };
        Context.StockBatches.Add(batch);
        return batch;
    }
}

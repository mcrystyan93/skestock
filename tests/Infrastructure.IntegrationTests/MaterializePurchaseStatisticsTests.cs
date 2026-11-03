using Microsoft.EntityFrameworkCore;
using skestock.Application.Features.Statistics.Commands.MaterializePurchaseStatistics;
using skestock.Domain.Entities;
using skestock.Domain.Entities.Categories;
using skestock.Domain.Entities.GoodsReceipts;
using skestock.Domain.Entities.Items;
using skestock.Domain.Entities.Locations;
using skestock.Domain.Entities.SchoolClasses;
using skestock.Domain.Entities.Stock;
using skestock.Domain.Entities.Users;
using skestock.Domain.Enums;
using skestock.Infrastructure.Identity;

namespace skestock.Infrastructure.IntegrationTests;

public sealed class MaterializePurchaseStatisticsTests
{
    private const string TimeZoneId = "Europe/Bucharest";

    private Guid _userId;
    private Guid _categoryId;
    private Guid _itemId;
    private Guid _classId;
    private Guid _locationId;
    private Guid _receiptId;
    private readonly List<Guid> _extraClassIds = [];
    private readonly List<Guid> _extraReceiptIds = [];

    [TearDown]
    public async Task TearDown()
    {
        await using var dbContext = IntegrationTestSetup.CreateDbContext();
        await dbContext.ItemPurchaseStatistics.Where(s => s.ItemId == _itemId).ExecuteDeleteAsync();
        await dbContext.StockTransactions.Where(t => t.ItemId == _itemId).ExecuteDeleteAsync();
        await dbContext.StockBatches.Where(b => b.ItemId == _itemId).ExecuteDeleteAsync();
        await dbContext.GoodsReceipts.Where(r => r.Id == _receiptId || _extraReceiptIds.Contains(r.Id)).ExecuteDeleteAsync();
        await dbContext.Items.Where(i => i.Id == _itemId).ExecuteDeleteAsync();
        await dbContext.Categories.Where(c => c.Id == _categoryId).ExecuteDeleteAsync();
        await dbContext.Locations.Where(l => l.Id == _locationId).ExecuteDeleteAsync();
        await dbContext.SchoolClasses.Where(c => c.Id == _classId || _extraClassIds.Contains(c.Id)).ExecuteDeleteAsync();
        await dbContext.UserProfiles.Where(u => u.IdentityId == _userId).ExecuteDeleteAsync();
        await dbContext.Users.Where(u => u.Id == _userId).ExecuteDeleteAsync();

        _extraClassIds.Clear();
        _extraReceiptIds.Clear();
    }

    [Test]
    public async Task Counts_only_goods_receipt_orders_as_purchases()
    {
        var receivedAt = DateTimeOffset.UtcNow.AddDays(-2);
        await SeedAsync(receivedAt);

        await using (var dbContext = IntegrationTestSetup.CreateDbContext())
        {
            var result = await new MaterializePurchaseStatisticsCommandHandler(dbContext, TimeProvider.System)
                .Handle(new MaterializePurchaseStatisticsCommand { TimeZoneId = TimeZoneId }, CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
        }

        await using var readContext = IntegrationTestSetup.CreateDbContext();
        var rows = await readContext.ItemPurchaseStatistics
            .AsNoTracking()
            .Where(s => s.ItemId == _itemId)
            .ToListAsync();

        rows.Select(r => r.Scope).ShouldBe(
            [PurchaseStatisticsScope.Last90Days, PurchaseStatisticsScope.Last365Days, PurchaseStatisticsScope.Class],
            ignoreOrder: true);
        rows.ShouldAllBe(r => r.TotalQuantity == 10
                              && r.TotalValue == 25.00m
                              && r.PurchaseCount == 1
                              && r.LastPurchasedAt == receivedAt);
    }

    [Test]
    public async Task Average_interval_excludes_the_break_between_classes()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        await SeedBreakAsync(today);

        await using (var dbContext = IntegrationTestSetup.CreateDbContext())
        {
            var result = await new MaterializePurchaseStatisticsCommandHandler(dbContext, TimeProvider.System)
                .Handle(new MaterializePurchaseStatisticsCommand { TimeZoneId = TimeZoneId }, CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
        }

        await using var readContext = IntegrationTestSetup.CreateDbContext();
        var interval = await readContext.ItemPurchaseStatistics
            .AsNoTracking()
            .Where(s => s.ItemId == _itemId && s.Scope == PurchaseStatisticsScope.Last365Days)
            .Select(s => s.AverageIntervalDays)
            .SingleAsync();

        // 6 weekly-in-class receipts, 49 days apart overall, 14 of them a break: 35 / 5 = 7.
        // Dates are relative to today; when a DST change falls inside the break (about 8% of run dates)
        // the result is 7.01 or 6.99, hence the tolerance.
        interval.ShouldNotBeNull();
        interval.Value.ShouldBe(7m, 0.01m);
    }

    // Class 1 ends 60 days ago, class 2 starts 45 days ago (14-day gap); three weekly receipts in each class.
    private async Task SeedBreakAsync(DateOnly today)
    {
        await using var dbContext = IntegrationTestSetup.CreateDbContext();

        var user = new ApplicationUser
        {
            UserName = $"purchase-{Guid.NewGuid():N}",
            Email = $"purchase-{Guid.NewGuid():N}@test.local"
        };
        dbContext.Users.Add(user);
        dbContext.UserProfiles.Add(new UserProfile
        {
            Id = Guid.CreateVersion7(),
            IdentityId = user.Id,
            FirstName = "Purchase",
            LastName = "Test"
        });

        var category = new Category { Id = Guid.CreateVersion7(), Name = $"Cat-{Guid.NewGuid():N}" };
        var item = new Item { Id = Guid.CreateVersion7(), Name = $"Item-{Guid.NewGuid():N}", CategoryId = category.Id };
        var location = new Location { Id = Guid.CreateVersion7(), Name = "Storage", Type = "StorageRoom" };
        var first = new SchoolClass
        {
            Id = Guid.CreateVersion7(),
            Name = $"Class-{Guid.NewGuid():N}",
            StartDate = today.AddDays(-200),
            EndDate = today.AddDays(-60)
        };
        var second = new SchoolClass
        {
            Id = Guid.CreateVersion7(),
            Name = $"Class-{Guid.NewGuid():N}",
            StartDate = today.AddDays(-45),
            EndDate = today.AddDays(200)
        };
        dbContext.AddRange(category, item, location, first, second);

        foreach (var (schoolClass, daysAgo) in new[]
                 {
                     (first, 74), (first, 67), (first, 60),
                     (second, 39), (second, 32), (second, 25)
                 })
        {
            var receivedAt = new DateTimeOffset(today.AddDays(-daysAgo).ToDateTime(new TimeOnly(9, 0)), TimeSpan.Zero);
            var receipt = new GoodsReceipt
            {
                Id = Guid.CreateVersion7(),
                ClassId = schoolClass.Id,
                Class = schoolClass,
                Note = "Receipt",
                ReceivedAt = receivedAt,
                TotalAmount = 25.00m
            };
            var batch = NewBatch(item.Id, location.Id, schoolClass.Id, 2.50m, receipt.Id);
            dbContext.AddRange(receipt, batch);
            dbContext.StockTransactions.Add(
                NewOrder(item.Id, location.Id, schoolClass.Id, user.Id, batch.Id, 10, receipt.Id, receivedAt));
            _extraReceiptIds.Add(receipt.Id);
        }

        await dbContext.SaveChangesAsync();

        (_userId, _categoryId, _itemId, _classId, _locationId) =
            (user.Id, category.Id, item.Id, first.Id, location.Id);
        _extraClassIds.Add(second.Id);
    }

    private async Task SeedAsync(DateTimeOffset receivedAt)
    {
        await using var dbContext = IntegrationTestSetup.CreateDbContext();

        var user = new ApplicationUser
        {
            UserName = $"purchase-{Guid.NewGuid():N}",
            Email = $"purchase-{Guid.NewGuid():N}@test.local"
        };
        dbContext.Users.Add(user);
        dbContext.UserProfiles.Add(new UserProfile
        {
            Id = Guid.CreateVersion7(),
            IdentityId = user.Id,
            FirstName = "Purchase",
            LastName = "Test"
        });

        var category = new Category { Id = Guid.CreateVersion7(), Name = $"Cat-{Guid.NewGuid():N}" };
        var item = new Item { Id = Guid.CreateVersion7(), Name = $"Item-{Guid.NewGuid():N}", CategoryId = category.Id };
        var schoolClass = new SchoolClass
        {
            Id = Guid.CreateVersion7(),
            Name = $"Class-{Guid.NewGuid():N}",
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-1)),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1))
        };
        var location = new Location { Id = Guid.CreateVersion7(), Name = "Storage", Type = "StorageRoom" };
        var receipt = new GoodsReceipt
        {
            Id = Guid.CreateVersion7(),
            ClassId = schoolClass.Id,
            Class = schoolClass,
            Note = "Receipt",
            ReceivedAt = receivedAt,
            TotalAmount = 25.00m
        };
        var receiptBatch = NewBatch(item.Id, location.Id, schoolClass.Id, 2.50m, receipt.Id);
        var manualBatch = NewBatch(item.Id, location.Id, schoolClass.Id, 4.00m, null);

        dbContext.AddRange(category, item, schoolClass, location, receipt, receiptBatch, manualBatch);
        dbContext.StockTransactions.AddRange(
            NewOrder(item.Id, location.Id, schoolClass.Id, user.Id, receiptBatch.Id, 10, receipt.Id, receivedAt),
            NewOrder(item.Id, location.Id, schoolClass.Id, user.Id, manualBatch.Id, 7, null, receivedAt));
        await dbContext.SaveChangesAsync();

        (_userId, _categoryId, _itemId, _classId, _locationId, _receiptId) =
            (user.Id, category.Id, item.Id, schoolClass.Id, location.Id, receipt.Id);
    }

    private static StockBatch NewBatch(
        Guid itemId, Guid locationId, Guid classId, decimal unitPrice, Guid? goodsReceiptId) => new()
    {
        Id = Guid.CreateVersion7(),
        ItemId = itemId,
        LocationId = locationId,
        ReceivedClassId = classId,
        Quantity = 100,
        ReceivedDate = DateOnly.FromDateTime(DateTime.UtcNow),
        UnitPrice = unitPrice,
        GoodsReceiptId = goodsReceiptId
    };

    private static StockTransaction NewOrder(
        Guid itemId, Guid locationId, Guid classId, Guid userId, Guid batchId,
        int quantity, Guid? goodsReceiptId, DateTimeOffset createdAt) => new()
    {
        Id = Guid.CreateVersion7(),
        ItemId = itemId,
        LocationId = locationId,
        ClassId = classId,
        BatchId = batchId,
        UserId = userId,
        Type = StockTransactionType.Order,
        QuantityChange = quantity,
        GoodsReceiptId = goodsReceiptId,
        CreatedAt = createdAt
    };
}

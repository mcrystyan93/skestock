using Microsoft.EntityFrameworkCore;
using skestock.Application.Features.Statistics.Commands.MaterializeDailyConsumption;
using skestock.Domain.Entities;
using skestock.Domain.Enums;
using skestock.Infrastructure.Identity;

namespace skestock.Infrastructure.IntegrationTests;

public sealed class MaterializeDailyConsumptionTests
{
    private const string TimeZoneId = "Europe/Bucharest";

    // A DST spring-forward day in Bucharest, far from any other test data.
    private static readonly DateOnly Day = new(2030, 3, 31);

    private Seed _seed = null!;

    [SetUp]
    public async Task SetUp()
    {
        _seed = await SeedAsync();
    }

    [TearDown]
    public async Task TearDown()
    {
        await using var dbContext = IntegrationTestSetup.CreateDbContext();
        await dbContext.DailyItemConsumptions
            .Where(c => c.ItemId == _seed.ItemId)
            .ExecuteDeleteAsync();
        await dbContext.StockTransactions.Where(t => t.ItemId == _seed.ItemId).ExecuteDeleteAsync();
        await dbContext.StockBatches.Where(b => b.ItemId == _seed.ItemId).ExecuteDeleteAsync();
        await dbContext.Items.Where(i => i.Id == _seed.ItemId).ExecuteDeleteAsync();
        await dbContext.Categories.Where(c => c.Id == _seed.CategoryId).ExecuteDeleteAsync();
        await dbContext.Locations
            .Where(l => l.Id == _seed.LocationId || l.Id == _seed.OtherLocationId)
            .ExecuteDeleteAsync();
        await dbContext.SchoolClasses.Where(c => c.Id == _seed.ClassId).ExecuteDeleteAsync();
        await dbContext.UserProfiles.Where(u => u.IdentityId == _seed.UserId).ExecuteDeleteAsync();
        await dbContext.Users.Where(u => u.Id == _seed.UserId).ExecuteDeleteAsync();
    }

    [Test]
    public async Task Aggregates_gross_non_transfer_consumption_per_local_day()
    {
        await AddTransactionsAsync(
            // Inside the local day (starts 2030-03-30T22:00Z, ends 2030-03-31T21:00Z after DST).
            Tx(_seed.LocationId, -3, StockTransactionType.Adjustment, Utc(2030, 3, 30, 22, 0)),
            Tx(_seed.LocationId, -2, StockTransactionType.Adjustment, Utc(2030, 3, 31, 20, 59)),
            Tx(_seed.OtherLocationId, -4, StockTransactionType.Adjustment, Utc(2030, 3, 31, 10, 0)),
            // Excluded: positive, transfer, and outside the local day.
            Tx(_seed.LocationId, 5, StockTransactionType.Adjustment, Utc(2030, 3, 31, 12, 0)),
            Tx(_seed.LocationId, -7, StockTransactionType.Transfer, Utc(2030, 3, 31, 12, 0)),
            Tx(_seed.LocationId, -1, StockTransactionType.Adjustment, Utc(2030, 3, 30, 21, 59)),
            Tx(_seed.LocationId, -1, StockTransactionType.Adjustment, Utc(2030, 3, 31, 21, 0)));

        var written = await MaterializeAsync(Day, Day);

        written.ShouldBe(2);
        var rows = await LoadRowsAsync();
        rows.Count.ShouldBe(2);

        var main = rows.Single(r => r.LocationId == _seed.LocationId);
        main.Date.ShouldBe(Day);
        main.ClassId.ShouldBe(_seed.ClassId);
        main.Quantity.ShouldBe(5);
        main.TotalValue.ShouldBe(12.50m);

        var other = rows.Single(r => r.LocationId == _seed.OtherLocationId);
        other.Quantity.ShouldBe(4);
        other.TotalValue.ShouldBe(10.00m);
    }

    [Test]
    public async Task Rerun_replaces_existing_rows_in_the_range()
    {
        await AddTransactionsAsync(
            Tx(_seed.LocationId, -3, StockTransactionType.Adjustment, Utc(2030, 3, 31, 10, 0)));
        await MaterializeAsync(Day, Day);

        await using (var dbContext = IntegrationTestSetup.CreateDbContext())
        {
            await dbContext.StockTransactions
                .Where(t => t.ItemId == _seed.ItemId)
                .ExecuteDeleteAsync();
        }

        await AddTransactionsAsync(
            Tx(_seed.OtherLocationId, -1, StockTransactionType.Adjustment, Utc(2030, 3, 31, 10, 0)));

        var written = await MaterializeAsync(Day, Day.AddDays(1));

        written.ShouldBe(1);
        var rows = await LoadRowsAsync();
        rows.Count.ShouldBe(1);
        rows[0].LocationId.ShouldBe(_seed.OtherLocationId);
        rows[0].Quantity.ShouldBe(1);
    }

    private async Task<int> MaterializeAsync(DateOnly from, DateOnly to)
    {
        await using var dbContext = IntegrationTestSetup.CreateDbContext();
        var handler = new MaterializeDailyConsumptionCommandHandler(dbContext, TimeProvider.System);

        var result = await handler.Handle(
            new MaterializeDailyConsumptionCommand
            {
                FromDate = from,
                ToDate = to,
                TimeZoneId = TimeZoneId
            },
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        return result.Value;
    }

    private async Task<List<DailyItemConsumption>> LoadRowsAsync()
    {
        await using var dbContext = IntegrationTestSetup.CreateDbContext();
        return await dbContext.DailyItemConsumptions
            .AsNoTracking()
            .Where(c => c.ItemId == _seed.ItemId)
            .ToListAsync();
    }

    private async Task AddTransactionsAsync(params StockTransaction[] transactions)
    {
        await using var dbContext = IntegrationTestSetup.CreateDbContext();
        dbContext.StockTransactions.AddRange(transactions);
        await dbContext.SaveChangesAsync();
    }

    private StockTransaction Tx(
        Guid locationId,
        int quantityChange,
        StockTransactionType type,
        DateTimeOffset createdAt) => new()
    {
        Id = Guid.CreateVersion7(),
        ItemId = _seed.ItemId,
        LocationId = locationId,
        ClassId = _seed.ClassId,
        BatchId = locationId == _seed.LocationId ? _seed.BatchId : _seed.OtherBatchId,
        UserId = _seed.UserId,
        Type = type,
        QuantityChange = quantityChange,
        CreatedAt = createdAt
    };

    private static DateTimeOffset Utc(int year, int month, int day, int hour, int minute) =>
        new(year, month, day, hour, minute, 0, TimeSpan.Zero);

    private static async Task<Seed> SeedAsync()
    {
        await using var dbContext = IntegrationTestSetup.CreateDbContext();

        var user = new ApplicationUser
        {
            UserName = $"consumption-{Guid.NewGuid():N}",
            Email = $"consumption-{Guid.NewGuid():N}@test.local"
        };
        dbContext.Users.Add(user);
        dbContext.UserProfiles.Add(new UserProfile
        {
            Id = Guid.CreateVersion7(),
            IdentityId = user.Id,
            FirstName = "Consumption",
            LastName = "Test"
        });

        var category = new Category { Id = Guid.CreateVersion7(), Name = $"Cat-{Guid.NewGuid():N}" };
        var item = new Item
        {
            Id = Guid.CreateVersion7(),
            Name = $"Item-{Guid.NewGuid():N}",
            CategoryId = category.Id
        };
        var schoolClass = new SchoolClass
        {
            Id = Guid.CreateVersion7(),
            Name = $"Class-{Guid.NewGuid():N}",
            StartDate = new DateOnly(2030, 1, 1),
            EndDate = new DateOnly(2030, 12, 31)
        };
        var location = new Location { Id = Guid.CreateVersion7(), Name = "Kitchen", Type = "Kitchen" };
        var otherLocation = new Location { Id = Guid.CreateVersion7(), Name = "Storage", Type = "StorageRoom" };

        var batch = NewBatch(item.Id, location.Id, schoolClass.Id, 2.50m);
        var otherBatch = NewBatch(item.Id, otherLocation.Id, schoolClass.Id, 2.50m);

        dbContext.AddRange(category, item, schoolClass, location, otherLocation, batch, otherBatch);
        await dbContext.SaveChangesAsync();

        return new Seed(
            user.Id,
            category.Id,
            item.Id,
            schoolClass.Id,
            location.Id,
            otherLocation.Id,
            batch.Id,
            otherBatch.Id);
    }

    private static StockBatch NewBatch(Guid itemId, Guid locationId, Guid classId, decimal unitPrice) => new()
    {
        Id = Guid.CreateVersion7(),
        ItemId = itemId,
        LocationId = locationId,
        ReceivedClassId = classId,
        Quantity = 100,
        ReceivedDate = new DateOnly(2030, 1, 1),
        UnitPrice = unitPrice
    };

    private sealed record Seed(
        Guid UserId,
        Guid CategoryId,
        Guid ItemId,
        Guid ClassId,
        Guid LocationId,
        Guid OtherLocationId,
        Guid BatchId,
        Guid OtherBatchId);
}

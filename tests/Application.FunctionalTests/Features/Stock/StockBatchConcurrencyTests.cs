using skestock.Domain.Entities;
using skestock.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace skestock.Application.FunctionalTests.Features.Stock;

/// <summary>
/// Proves the <see cref="StockBatch.Version"/> rowversion token enforces optimistic concurrency
/// against real SQL Server: two contexts load the same batch, the first save wins, and the second
/// (now working from a stale rowversion) is rejected with <see cref="DbUpdateConcurrencyException"/>.
/// This is the DB-level guarantee the AdjustStock/MoveStock/RemoveExpiredStock handlers rely on to
/// translate lost updates into a typed 409 conflict.
/// </summary>
public class StockBatchConcurrencyTests : TestBase
{
    [Test]
    public async Task StaleUpdate_AfterConcurrentWrite_ThrowsConcurrencyException()
    {
        var category = new Category { Name = $"FT{Guid.NewGuid():N}"[..10] };
        await TestApp.AddAsync(category);
        var item = new Item
        {
            Name = "Flour",
            Unit = "kg",
            MinThreshold = 5,
            CategoryId = category.Id
        };
        await TestApp.AddAsync(item);
        var location = new Location { Name = "Storage", Type = "StorageRoom" };
        await TestApp.AddAsync(location);
        var schoolClass = new SchoolClass
        {
            Name = "Fall 2026",
            StartDate = new DateOnly(2026, 9, 1),
            EndDate = new DateOnly(2026, 12, 20)
        };
        await TestApp.AddAsync(schoolClass);
        var batch = new StockBatch
        {
            ItemId = item.Id,
            LocationId = location.Id,
            ReceivedClassId = schoolClass.Id,
            Quantity = 10,
            UnitPrice = 1m,
            ReceivedDate = new DateOnly(2026, 1, 1)
        };
        await TestApp.AddAsync(batch);

        using var scopeA = FunctionalTestSetup.ScopeFactory.CreateScope();
        using var scopeB = FunctionalTestSetup.ScopeFactory.CreateScope();
        var contextA = scopeA.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var contextB = scopeB.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Both readers load the same row at the same rowversion.
        var batchA = await contextA.StockBatches.SingleAsync(b => b.Id == batch.Id);
        var batchB = await contextB.StockBatches.SingleAsync(b => b.Id == batch.Id);

        // First writer wins; SQL Server bumps the rowversion.
        batchB.Quantity = 5;
        await contextB.SaveChangesAsync();

        // Second writer is now stale: its rowversion no longer matches the row.
        batchA.Quantity = 8;
        await Should.ThrowAsync<DbUpdateConcurrencyException>(() => contextA.SaveChangesAsync());

        var persisted = await TestApp.SingleOrDefaultAsync<StockBatch>(b => b.Id == batch.Id);
        persisted!.Quantity.ShouldBe(5);
    }
}

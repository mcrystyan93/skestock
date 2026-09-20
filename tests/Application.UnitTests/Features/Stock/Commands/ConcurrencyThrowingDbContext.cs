using Microsoft.EntityFrameworkCore;
using skestock.Application.UnitTests.Features.GoodsReceipts.Commands.CreateGoodsReceipt;

namespace skestock.Application.UnitTests.Features.Stock.Commands;

/// <summary>
/// Test double that lets a seeded save succeed but forces the next <see cref="SaveChangesAsync"/>
/// to raise <see cref="DbUpdateConcurrencyException"/>, mimicking the SQL Server rowversion token
/// on <c>StockBatch</c> losing a race. The EF Core in-memory provider does not enforce concurrency
/// tokens, so this is the only way to exercise the handlers' conflict-handling path in a unit test.
/// </summary>
public sealed class ConcurrencyThrowingDbContext(DbContextOptions<GoodsReceiptTestDbContext> options)
    : GoodsReceiptTestDbContext(options)
{
    public bool ThrowOnNextSave { get; set; }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (ThrowOnNextSave)
        {
            throw new DbUpdateConcurrencyException(
                "Simulated optimistic-concurrency conflict on StockBatch.");
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}

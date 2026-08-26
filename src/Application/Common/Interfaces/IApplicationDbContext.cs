using skestock.Domain.Entities;

namespace skestock.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Category> Categories { get; }
    DbSet<ClassBalance> ClassBalances { get; }
    DbSet<Item> Items { get; }
    DbSet<Location> Locations { get; }
    DbSet<SchoolClass> SchoolClasses { get; }
    DbSet<StockBatch> StockBatches { get; }
    DbSet<StockTransaction> StockTransactions { get; }
    DbSet<GoodsReceipt> GoodsReceipts { get; }
    DbSet<UserProfile> UserProfiles { get; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}

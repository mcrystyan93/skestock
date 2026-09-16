using Microsoft.EntityFrameworkCore.Infrastructure;
using skestock.Domain.Entities;
using skestock.Domain.Queues;

namespace skestock.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DatabaseFacade Database { get; }
    DbSet<Category> Categories { get; }
    DbSet<CategoryImportBatch> CategoryImportBatches { get; }
    DbSet<CategoryImportBatchFile> CategoryImportBatchFiles { get; }
    DbSet<ClassBalance> ClassBalances { get; }
    DbSet<ClassItemStockVisibility> ClassItemStockVisibilities =>
        throw new NotSupportedException("This test context does not expose class-item stock visibility.");
    DbSet<FileMetadata> FileMetadata { get; }
    DbSet<Item> Items { get; }
    DbSet<ItemImportBatch> ItemImportBatches { get; }
    DbSet<ItemImportBatchFile> ItemImportBatchFiles { get; }
    DbSet<Location> Locations { get; }
    DbSet<SchoolClass> SchoolClasses { get; }
    DbSet<StockBatch> StockBatches { get; }
    DbSet<StockTransaction> StockTransactions { get; }
    DbSet<GoodsReceipt> GoodsReceipts { get; }
    DbSet<GoodsReceiptImport> GoodsReceiptImports { get; }
    DbSet<GoodsReceiptImportLine> GoodsReceiptImportLines { get; }
    DbSet<UserProfile> UserProfiles { get; }
    DbSet<OutboxMessage> OutboxMessages { get; }
    DbSet<ProcessedMessage> ProcessedMessages { get; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}

using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using skestock.Domain.Entities;
using skestock.Domain.Entities.Categories;
using skestock.Domain.Entities.GoodsReceipts;
using skestock.Domain.Entities.Items;
using skestock.Domain.Entities.Locations;
using skestock.Domain.Entities.OrderLists;
using skestock.Domain.Entities.SchoolClasses;
using skestock.Domain.Entities.Statistics;
using skestock.Domain.Entities.Stock;
using skestock.Domain.Entities.Storage;
using skestock.Domain.Entities.SupplyLists;
using skestock.Domain.Entities.Users;
using skestock.Domain.Queues;

namespace skestock.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DatabaseFacade Database { get; }
    ChangeTracker ChangeTracker { get; }
    DbSet<Category> Categories { get; }
    DbSet<CategoryImportBatch> CategoryImportBatches { get; }
    DbSet<CategoryImportBatchFile> CategoryImportBatchFiles { get; }
    DbSet<ClassBalance> ClassBalances { get; }
    DbSet<ClassItemStockVisibility> ClassItemStockVisibilities { get; }
    DbSet<DailyItemConsumption> DailyItemConsumptions { get; }
    DbSet<ItemPurchaseStatistic> ItemPurchaseStatistics { get; }
    DbSet<FileMetadata> FileMetadata { get; }
    DbSet<Item> Items { get; }
    DbSet<ItemImportBatch> ItemImportBatches { get; }
    DbSet<ItemImportBatchFile> ItemImportBatchFiles { get; }
    DbSet<Location> Locations { get; }
    DbSet<SchoolClass> SchoolClasses { get; }

    DbSet<SharedClassConfiguration> SharedClassConfigurations =>
        throw new NotSupportedException("This test context does not support shared class configuration.");

    DbSet<StockBatch> StockBatches { get; }
    DbSet<StockTransaction> StockTransactions { get; }
    DbSet<GoodsReceipt> GoodsReceipts { get; }
    DbSet<GoodsReceiptImport> GoodsReceiptImports { get; }
    DbSet<GoodsReceiptImportLine> GoodsReceiptImportLines { get; }
    DbSet<OrderList> OrderLists { get; }
    DbSet<OrderListLine> OrderListLines { get; }
    DbSet<SupplyList> SupplyLists { get; }
    DbSet<SupplyListLine> SupplyListLines { get; }
    DbSet<UserProfile> UserProfiles { get; }
    DbSet<OutboxMessage> OutboxMessages { get; }
    DbSet<ProcessedMessage> ProcessedMessages { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}

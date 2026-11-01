using Microsoft.EntityFrameworkCore;
using skestock.Application.Common.Interfaces;
using skestock.Domain.Entities;
using skestock.Domain.Entities.Categories;
using skestock.Domain.Entities.Congregations;
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

namespace skestock.Application.UnitTests.Features.Congregations;

/// <summary>
/// Minimal <see cref="IApplicationDbContext"/> for Congregation tests. Maps only
/// <see cref="Congregation"/> (plus the audit-author `UserProfile`); every other required DbSet throws
/// and its entity is ignored.
/// </summary>
public class CongregationTestDbContext(DbContextOptions<CongregationTestDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<Congregation> Congregations => Set<Congregation>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    DbSet<Category> IApplicationDbContext.Categories => throw new NotSupportedException();
    DbSet<CategoryImportBatch> IApplicationDbContext.CategoryImportBatches => throw new NotSupportedException();
    DbSet<CategoryImportBatchFile> IApplicationDbContext.CategoryImportBatchFiles => throw new NotSupportedException();
    DbSet<ClassBalance> IApplicationDbContext.ClassBalances => throw new NotSupportedException();
    DbSet<ClassItemStockVisibility> IApplicationDbContext.ClassItemStockVisibilities => throw new NotSupportedException();
    DbSet<DailyItemConsumption> IApplicationDbContext.DailyItemConsumptions => throw new NotSupportedException();
    DbSet<ItemPurchaseStatistic> IApplicationDbContext.ItemPurchaseStatistics => throw new NotSupportedException();
    DbSet<FileMetadata> IApplicationDbContext.FileMetadata => throw new NotSupportedException();
    DbSet<Item> IApplicationDbContext.Items => throw new NotSupportedException();
    DbSet<ItemImportBatch> IApplicationDbContext.ItemImportBatches => throw new NotSupportedException();
    DbSet<ItemImportBatchFile> IApplicationDbContext.ItemImportBatchFiles => throw new NotSupportedException();
    DbSet<Location> IApplicationDbContext.Locations => throw new NotSupportedException();
    DbSet<SchoolClass> IApplicationDbContext.SchoolClasses => throw new NotSupportedException();
    DbSet<StockBatch> IApplicationDbContext.StockBatches => throw new NotSupportedException();
    DbSet<StockTransaction> IApplicationDbContext.StockTransactions => throw new NotSupportedException();
    DbSet<GoodsReceipt> IApplicationDbContext.GoodsReceipts => throw new NotSupportedException();
    DbSet<GoodsReceiptImport> IApplicationDbContext.GoodsReceiptImports => throw new NotSupportedException();
    DbSet<GoodsReceiptImportLine> IApplicationDbContext.GoodsReceiptImportLines => throw new NotSupportedException();
    DbSet<OrderList> IApplicationDbContext.OrderLists => throw new NotSupportedException();
    DbSet<OrderListLine> IApplicationDbContext.OrderListLines => throw new NotSupportedException();
    DbSet<SupplyList> IApplicationDbContext.SupplyLists => throw new NotSupportedException();
    DbSet<SupplyListLine> IApplicationDbContext.SupplyListLines => throw new NotSupportedException();

    DbSet<OutboxMessage> IApplicationDbContext.OutboxMessages => throw new NotSupportedException();
    DbSet<ProcessedMessage> IApplicationDbContext.ProcessedMessages => throw new NotSupportedException();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // EF discovers every DbSet property (even explicit ones), so unused entities must be ignored.
        builder.Ignore<Category>();
        builder.Ignore<CategoryImportBatch>();
        builder.Ignore<CategoryImportBatchFile>();
        builder.Ignore<ClassBalance>();
        builder.Ignore<ClassItemStockVisibility>();
        builder.Ignore<DailyItemConsumption>();
        builder.Ignore<ItemPurchaseStatistic>();
        builder.Ignore<FileMetadata>();
        builder.Ignore<Item>();
        builder.Ignore<ItemImportBatch>();
        builder.Ignore<ItemImportBatchFile>();
        builder.Ignore<Location>();
        builder.Ignore<SchoolClass>();
        builder.Ignore<StockBatch>();
        builder.Ignore<StockTransaction>();
        builder.Ignore<GoodsReceipt>();
        builder.Ignore<GoodsReceiptImport>();
        builder.Ignore<GoodsReceiptImportLine>();
        builder.Ignore<OrderList>();
        builder.Ignore<OrderListLine>();
        builder.Ignore<SupplyList>();
        builder.Ignore<SupplyListLine>();
        builder.Ignore<OutboxMessage>();
        builder.Ignore<ProcessedMessage>();

        builder.Entity<UserProfile>(b =>
        {
            b.Ignore(u => u.CreatedBy);
            b.Ignore(u => u.LastModifiedBy);
            b.Ignore(u => u.Transactions);
            b.Ignore(u => u.GoodsReceipts);
        });

        builder.Entity<Congregation>(b =>
        {
            b.HasOne(c => c.CreatedBy).WithMany().HasForeignKey(c => c.CreatedById);
            b.HasOne(c => c.LastModifiedBy).WithMany().HasForeignKey(c => c.LastModifiedById);
            b.HasQueryFilter(c => c.DeletedDate == null);
        });
    }
}

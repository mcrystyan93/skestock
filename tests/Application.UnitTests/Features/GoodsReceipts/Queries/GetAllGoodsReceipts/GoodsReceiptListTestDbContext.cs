using Microsoft.EntityFrameworkCore;
using skestock.Application.Common.Interfaces;
using skestock.Domain.Entities;
using skestock.Domain.Queues;

namespace skestock.Application.UnitTests.Features.GoodsReceipts.Queries.GetAllGoodsReceipts;

public class GoodsReceiptListTestDbContext(DbContextOptions<GoodsReceiptListTestDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<CategoryImportBatch> CategoryImportBatches => Set<CategoryImportBatch>();
    public DbSet<CategoryImportBatchFile> CategoryImportBatchFiles => Set<CategoryImportBatchFile>();
    public DbSet<ClassBalance> ClassBalances => Set<ClassBalance>();
    public DbSet<ClassItemStockVisibility> ClassItemStockVisibilities => Set<ClassItemStockVisibility>();
    public DbSet<DailyItemConsumption> DailyItemConsumptions => Set<DailyItemConsumption>();
    public DbSet<ItemPurchaseStatistic> ItemPurchaseStatistics => Set<ItemPurchaseStatistic>();
    public DbSet<FileMetadata> FileMetadata => Set<FileMetadata>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<ItemImportBatch> ItemImportBatches => Set<ItemImportBatch>();
    public DbSet<ItemImportBatchFile> ItemImportBatchFiles => Set<ItemImportBatchFile>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<SchoolClass> SchoolClasses => Set<SchoolClass>();
    public DbSet<StockBatch> StockBatches => Set<StockBatch>();
    public DbSet<StockTransaction> StockTransactions => Set<StockTransaction>();
    public DbSet<GoodsReceipt> GoodsReceipts => Set<GoodsReceipt>();
    public DbSet<GoodsReceiptImport> GoodsReceiptImports => Set<GoodsReceiptImport>();
    public DbSet<GoodsReceiptImportLine> GoodsReceiptImportLines => Set<GoodsReceiptImportLine>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<ProcessedMessage> ProcessedMessages => Set<ProcessedMessage>();
    public DbSet<OrderList> OrderLists => Set<OrderList>();
    public DbSet<OrderListLine> OrderListLines => Set<OrderListLine>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Ignore<DailyItemConsumption>();

        builder.Ignore<OrderList>();
        builder.Ignore<OrderListLine>();

        builder.Ignore<Category>();
        builder.Ignore<CategoryImportBatch>();
        builder.Ignore<CategoryImportBatchFile>();
        builder.Ignore<ClassBalance>();
        builder.Ignore<Item>();
        builder.Ignore<ItemImportBatch>();
        builder.Ignore<ItemImportBatchFile>();
        builder.Ignore<Location>();
        builder.Ignore<StockTransaction>();
        builder.Ignore<GoodsReceiptImportLine>();
        builder.Ignore<OutboxMessage>();
        builder.Ignore<ProcessedMessage>();

        builder.Entity<UserProfile>(b =>
        {
            b.Ignore(u => u.CreatedBy);
            b.Ignore(u => u.LastModifiedBy);
            b.Ignore(u => u.Transactions);
            b.Ignore(u => u.GoodsReceipts);
        });

        builder.Entity<FileMetadata>(b =>
        {
            b.Ignore(f => f.CreatedBy);
            b.Ignore(f => f.LastModifiedBy);
        });

        builder.Entity<SchoolClass>(b =>
        {
            b.Ignore(c => c.CreatedBy);
            b.Ignore(c => c.LastModifiedBy);
            b.Ignore(c => c.BatchesReceived);
            b.Ignore(c => c.Transactions);
            b.Ignore(c => c.Balances);
            b.Ignore(c => c.GoodsReceipts);
        });

        builder.Entity<GoodsReceipt>(b =>
        {
            b.HasOne(r => r.Class).WithMany().HasForeignKey(r => r.ClassId);
            b.HasOne(r => r.CreatedBy).WithMany().HasForeignKey(r => r.CreatedById);
            b.HasOne(r => r.LastModifiedBy).WithMany().HasForeignKey(r => r.LastModifiedById);
            b.Ignore(r => r.Transactions);
        });

        builder.Entity<StockBatch>(b =>
        {
            b.HasOne(sb => sb.GoodsReceipt).WithMany(r => r.Batches).HasForeignKey(sb => sb.GoodsReceiptId);
            b.Ignore(sb => sb.Item);
            b.Ignore(sb => sb.Location);
            b.Ignore(sb => sb.ReceivedClass);
            b.Ignore(sb => sb.Transactions);
            b.Ignore(sb => sb.CreatedBy);
            b.Ignore(sb => sb.LastModifiedBy);
        });

        builder.Entity<GoodsReceiptImport>(b =>
        {
            b.HasOne(i => i.FileMetadata).WithMany().HasForeignKey(i => i.FileMetadataId);
            b.HasOne(i => i.ResultingGoodsReceipt).WithMany().HasForeignKey(i => i.ResultingGoodsReceiptId);
            b.Ignore(i => i.Class);
            b.Ignore(i => i.UploadedByUser);
            b.Ignore(i => i.Lines);
            b.Ignore(i => i.CreatedBy);
            b.Ignore(i => i.LastModifiedBy);
        });
    }
}

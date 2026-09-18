using Microsoft.EntityFrameworkCore;
using skestock.Application.Common.Interfaces;
using skestock.Domain.Entities;
using skestock.Domain.Queues;

namespace skestock.Application.UnitTests.Features.OrderLists;

public class OrderListTestDbContext(DbContextOptions<OrderListTestDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<CategoryImportBatch> CategoryImportBatches => Set<CategoryImportBatch>();
    public DbSet<CategoryImportBatchFile> CategoryImportBatchFiles => Set<CategoryImportBatchFile>();
    public DbSet<ClassBalance> ClassBalances => Set<ClassBalance>();
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

        builder.Ignore<CategoryImportBatch>();
        builder.Ignore<CategoryImportBatchFile>();
        builder.Ignore<ClassBalance>();
        builder.Ignore<ItemImportBatch>();
        builder.Ignore<ItemImportBatchFile>();
        builder.Ignore<Location>();
        builder.Ignore<StockBatch>();
        builder.Ignore<StockTransaction>();
        builder.Ignore<GoodsReceipt>();
        builder.Ignore<GoodsReceiptImport>();
        builder.Ignore<GoodsReceiptImportLine>();
        builder.Ignore<FileMetadata>();
        builder.Ignore<OutboxMessage>();
        builder.Ignore<ProcessedMessage>();

        builder.Entity<Category>(b =>
        {
            b.HasOne(c => c.CreatedBy).WithMany().HasForeignKey(c => c.CreatedById);
            b.HasOne(c => c.LastModifiedBy).WithMany().HasForeignKey(c => c.LastModifiedById);
            b.OwnsOne(c => c.Icon);
        });

        builder.Entity<Item>(b =>
        {
            b.HasOne(i => i.CreatedBy).WithMany().HasForeignKey(i => i.CreatedById);
            b.HasOne(i => i.LastModifiedBy).WithMany().HasForeignKey(i => i.LastModifiedById);
            b.HasOne(i => i.Category).WithMany(c => c.Items).HasForeignKey(i => i.CategoryId);
            b.Ignore(i => i.ClassBalances);
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

        builder.Entity<UserProfile>(b =>
        {
            b.Ignore(u => u.CreatedBy);
            b.Ignore(u => u.LastModifiedBy);
            b.Ignore(u => u.Transactions);
            b.Ignore(u => u.GoodsReceipts);
        });

        builder.Entity<OrderList>(b =>
        {
            b.HasOne(o => o.Class).WithMany().HasForeignKey(o => o.ClassId);
            b.HasMany(o => o.Lines).WithOne(l => l.OrderList).HasForeignKey(l => l.OrderListId);
            b.HasOne(o => o.CreatedBy).WithMany().HasForeignKey(o => o.CreatedById);
            b.HasOne(o => o.LastModifiedBy).WithMany().HasForeignKey(o => o.LastModifiedById);
        });

        builder.Entity<OrderListLine>(b =>
        {
            b.HasOne(l => l.Item).WithMany().HasForeignKey(l => l.ItemId);
            b.Ignore(l => l.CreatedBy);
            b.Ignore(l => l.LastModifiedBy);
        });
    }
}

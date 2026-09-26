using Microsoft.EntityFrameworkCore;
using skestock.Application.Common.Interfaces;
using skestock.Domain.Entities;
using skestock.Domain.Queues;

namespace skestock.Application.UnitTests.Features.Categories.Commands.ProcessCategoryImportBatch;

/// <summary>
/// Minimal <see cref="IApplicationDbContext"/> implementation for ProcessCategoryImportBatch handler
/// tests. Maps <see cref="CategoryImportBatch"/> plus the <see cref="FileMetadata"/> it references;
/// everything else is ignored so EF's in-memory model stays unambiguous.
/// </summary>
public class ProcessCategoryImportBatchTestDbContext(DbContextOptions<ProcessCategoryImportBatchTestDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<CategoryImportBatch> CategoryImportBatches => Set<CategoryImportBatch>();
    public DbSet<CategoryImportBatchFile> CategoryImportBatchFiles => Set<CategoryImportBatchFile>();
    public DbSet<ClassBalance> ClassBalances => Set<ClassBalance>();
    public DbSet<ClassItemStockVisibility> ClassItemStockVisibilities => Set<ClassItemStockVisibility>();
    public DbSet<DailyItemConsumption> DailyItemConsumptions => Set<DailyItemConsumption>();
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
    public DbSet<FileMetadata> FileMetadata => Set<FileMetadata>();
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

        builder.Entity<CategoryImportBatch>(b =>
        {
            b.HasOne(i => i.UploadedByUser).WithMany().HasForeignKey(i => i.UploadedByUserId).HasPrincipalKey(u => u.IdentityId);
            b.HasOne(i => i.CreatedBy).WithMany().HasForeignKey(i => i.CreatedById).HasPrincipalKey(u => u.IdentityId);
            b.HasOne(i => i.LastModifiedBy).WithMany().HasForeignKey(i => i.LastModifiedById).HasPrincipalKey(u => u.IdentityId);
            b.OwnsMany(batch => batch.History, history =>
            {
                history.HasKey(entry => entry.Id);
                history.Property(entry => entry.Id).ValueGeneratedNever();
            });
        });
        builder.Entity<CategoryImportBatchFile>(b =>
        {
            b.HasOne(file => file.Batch).WithMany(batch => batch.Files)
                .HasForeignKey(file => file.CategoryImportBatchId);
            b.HasOne(file => file.FileMetadata).WithMany()
                .HasForeignKey(file => file.FileMetadataId);
        });
        builder.Ignore<ItemImportBatch>();
        builder.Ignore<ItemImportBatchFile>();

        builder.Ignore<GoodsReceiptImport>();
        builder.Ignore<GoodsReceiptImportLine>();
        builder.Ignore<GoodsReceipt>();
        builder.Ignore<StockBatch>();
        builder.Ignore<StockTransaction>();
        builder.Ignore<ClassBalance>();
        builder.Ignore<Item>();
        builder.Ignore<Location>();
        builder.Ignore<SchoolClass>();
        builder.Entity<Category>(b =>
        {
            b.HasOne(c => c.CreatedBy).WithMany().HasForeignKey(c => c.CreatedById);
            b.HasOne(c => c.LastModifiedBy).WithMany().HasForeignKey(c => c.LastModifiedById);
            b.OwnsOne(c => c.Icon);
            b.Ignore(c => c.Items);
        });
    }
}

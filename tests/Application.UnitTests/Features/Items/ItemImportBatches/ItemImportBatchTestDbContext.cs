using Microsoft.EntityFrameworkCore;
using skestock.Application.Common.Interfaces;
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

namespace skestock.Application.UnitTests.Features.Items.ItemImportBatches;

public sealed class ItemImportBatchTestDbContext(DbContextOptions<ItemImportBatchTestDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<CategoryImportBatch> CategoryImportBatches => Set<CategoryImportBatch>();
    public DbSet<CategoryImportBatchFile> CategoryImportBatchFiles => Set<CategoryImportBatchFile>();
    public DbSet<ClassBalance> ClassBalances => Set<ClassBalance>();
    public DbSet<ClassItemStockVisibility> ClassItemStockVisibilities => Set<ClassItemStockVisibility>();
    public DbSet<DailyItemConsumption> DailyItemConsumptions => Set<DailyItemConsumption>();
    public DbSet<ItemPurchaseStatistic> ItemPurchaseStatistics => Set<ItemPurchaseStatistic>();
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
    public DbSet<OrderList> OrderLists => Set<OrderList>();
    public DbSet<OrderListLine> OrderListLines => Set<OrderListLine>();
    public DbSet<SupplyList> SupplyLists => Set<SupplyList>();
    public DbSet<SupplyListLine> SupplyListLines => Set<SupplyListLine>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<FileMetadata> FileMetadata => Set<FileMetadata>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<ProcessedMessage> ProcessedMessages => Set<ProcessedMessage>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Ignore<CategoryImportBatch>();
        builder.Ignore<CategoryImportBatchFile>();
        builder.Ignore<GoodsReceiptImport>();
        builder.Ignore<GoodsReceiptImportLine>();
        builder.Ignore<GoodsReceipt>();
        builder.Ignore<DailyItemConsumption>();
        builder.Ignore<OrderList>();
        builder.Ignore<OrderListLine>();
        builder.Ignore<SchoolClass>();
        builder.Ignore<Location>();
        builder.Ignore<StockBatch>();
        builder.Ignore<StockTransaction>();
        builder.Ignore<ClassBalance>();

        builder.Entity<UserProfile>(profile =>
        {
            profile.Ignore(user => user.CreatedBy);
            profile.Ignore(user => user.LastModifiedBy);
            profile.Ignore(user => user.Transactions);
            profile.Ignore(user => user.GoodsReceipts);
        });
        builder.Entity<FileMetadata>(file =>
        {
            file.Ignore(metadata => metadata.CreatedBy);
            file.Ignore(metadata => metadata.LastModifiedBy);
        });
        builder.Entity<Category>(category =>
        {
            category.Ignore(entity => entity.CreatedBy);
            category.Ignore(entity => entity.LastModifiedBy);
            category.HasMany(entity => entity.Items).WithOne(item => item.Category)
                .HasForeignKey(item => item.CategoryId);
            category.OwnsOne(entity => entity.Icon);
        });
        builder.Entity<Item>(item =>
        {
            item.Ignore(entity => entity.CreatedBy);
            item.Ignore(entity => entity.LastModifiedBy);
            item.Ignore(entity => entity.Batches);
            item.Ignore(entity => entity.Transactions);
            item.Ignore(entity => entity.ClassBalances);
        });
        builder.Entity<ItemImportBatch>(batch =>
        {
            batch.HasOne(entity => entity.UploadedByUser).WithMany()
                .HasForeignKey(entity => entity.UploadedByUserId)
                .HasPrincipalKey(user => user.IdentityId);
            batch.Ignore(entity => entity.CreatedBy);
            batch.Ignore(entity => entity.LastModifiedBy);
            batch.OwnsMany(entity => entity.History, history =>
            {
                history.HasKey(entry => entry.Id);
                history.Property(entry => entry.Id).ValueGeneratedNever();
            });
        });
        builder.Entity<ItemImportBatchFile>(file =>
        {
            file.HasOne(entity => entity.Batch).WithMany(batch => batch.Files)
                .HasForeignKey(entity => entity.ItemImportBatchId);
            file.HasOne(entity => entity.FileMetadata).WithMany()
                .HasForeignKey(entity => entity.FileMetadataId);
        });
    }
}

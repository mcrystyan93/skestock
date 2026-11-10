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

namespace skestock.Application.UnitTests.Features.Items.Commands.MergeItems;

/// <summary>
/// In-memory <see cref="IApplicationDbContext"/> that keeps every entity carrying an ItemId (the existing
/// ItemTestDbContext ignores most of them) and ignores the rest of the graph.
/// </summary>
public class MergeTestDbContext(DbContextOptions<MergeTestDbContext> options)
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
    public DbSet<ItemLocationThreshold> ItemLocationThresholds => Set<ItemLocationThreshold>();
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
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<ProcessedMessage> ProcessedMessages => Set<ProcessedMessage>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Ignore<Category>();
        builder.Ignore<CategoryImportBatch>();
        builder.Ignore<CategoryImportBatchFile>();
        builder.Ignore<FileMetadata>();
        builder.Ignore<ItemImportBatch>();
        builder.Ignore<ItemImportBatchFile>();
        builder.Ignore<Location>();
        builder.Ignore<GoodsReceipt>();
        builder.Ignore<GoodsReceiptImport>();
        builder.Ignore<OrderList>();
        builder.Ignore<SupplyList>();
        builder.Ignore<UserProfile>();
        builder.Ignore<OutboxMessage>();
        builder.Ignore<ProcessedMessage>();
    }
}

internal static class MergeTestData
{
    public static MergeTestDbContext NewContext() =>
        new(new DbContextOptionsBuilder<MergeTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    public static Item NewItem(string name, bool isActive = true) =>
        new() { Name = name, Unit = "unit", IsActive = isActive };
}

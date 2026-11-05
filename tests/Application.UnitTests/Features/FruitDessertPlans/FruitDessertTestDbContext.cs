using Microsoft.EntityFrameworkCore;
using skestock.Application.Common.Interfaces;
using skestock.Domain.Entities;
using skestock.Domain.Entities.Categories;
using skestock.Domain.Entities.Congregations;
using skestock.Domain.Entities.FruitDessert;
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

namespace skestock.Application.UnitTests.Features.FruitDessertPlans;

/// <summary>
/// Minimal <see cref="IApplicationDbContext"/> for fruit and dessert planning tests. Maps only
/// the plan entities, <see cref="SchoolClass"/> and <see cref="Congregation"/>; every other required DbSet throws
/// and its entity is ignored.
/// </summary>
public class FruitDessertTestDbContext(DbContextOptions<FruitDessertTestDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    public static FruitDessertTestDbContext Create() =>
        new(new DbContextOptionsBuilder<FruitDessertTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    public DbSet<Congregation> Congregations => Set<Congregation>();
    public DbSet<SchoolClass> SchoolClasses => Set<SchoolClass>();
    public DbSet<FruitDessertPlanEntry> FruitDessertPlanEntries => Set<FruitDessertPlanEntry>();
    public DbSet<FruitDessertMessage> FruitDessertMessages => Set<FruitDessertMessage>();
    public DbSet<MessageTemplate> MessageTemplates => Set<MessageTemplate>();
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
    DbSet<StockBatch> IApplicationDbContext.StockBatches => throw new NotSupportedException();
    DbSet<StockTransaction> IApplicationDbContext.StockTransactions => throw new NotSupportedException();
    DbSet<GoodsReceipt> IApplicationDbContext.GoodsReceipts => throw new NotSupportedException();
    DbSet<GoodsReceiptImport> IApplicationDbContext.GoodsReceiptImports => throw new NotSupportedException();
    DbSet<GoodsReceiptImportLine> IApplicationDbContext.GoodsReceiptImportLines => throw new NotSupportedException();
    DbSet<OrderList> IApplicationDbContext.OrderLists => throw new NotSupportedException();
    DbSet<OrderListLine> IApplicationDbContext.OrderListLines => throw new NotSupportedException();
    DbSet<SupplyList> IApplicationDbContext.SupplyLists => throw new NotSupportedException();
    DbSet<SupplyListLine> IApplicationDbContext.SupplyListLines => throw new NotSupportedException();

    DbSet<UserProfile> IApplicationDbContext.UserProfiles => throw new NotSupportedException();
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
        builder.Ignore<ClassDepartment>();
        builder.Ignore<UserProfile>();
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

        builder.Entity<SchoolClass>(b =>
        {
            b.Ignore(c => c.Departments);
            b.Ignore(c => c.BatchesReceived);
            b.Ignore(c => c.Transactions);
            b.Ignore(c => c.Balances);
            b.Ignore(c => c.GoodsReceipts);
            b.Ignore(c => c.CreatedBy);
            b.Ignore(c => c.LastModifiedBy);
        });

        builder.Entity<Congregation>(b =>
        {
            b.Ignore(c => c.CreatedBy);
            b.Ignore(c => c.LastModifiedBy);
            b.HasQueryFilter(c => c.DeletedDate == null);
        });

        builder.Entity<FruitDessertPlanEntry>(b =>
        {
            b.Ignore(e => e.CreatedBy);
            b.Ignore(e => e.LastModifiedBy);
            b.HasOne(e => e.Congregation).WithMany().HasForeignKey(e => e.CongregationId);
        });

        builder.Entity<FruitDessertMessage>(b =>
        {
            b.Ignore(m => m.CreatedBy);
            b.Ignore(m => m.LastModifiedBy);
        });

        builder.Entity<MessageTemplate>(b =>
        {
            b.Ignore(t => t.CreatedBy);
            b.Ignore(t => t.LastModifiedBy);
        });
    }
}

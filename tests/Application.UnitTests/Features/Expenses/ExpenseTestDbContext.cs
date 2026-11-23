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

namespace skestock.Application.UnitTests.Features.Expenses;

/// <summary>
/// Minimal <see cref="IApplicationDbContext"/> implementation for Expense
/// handler tests. Maps Expense plus the <see cref="FileMetadata"/> and class it
/// references; everything else is ignored so EF's in-memory model stays unambiguous.
/// </summary>
public class ExpenseTestDbContext(DbContextOptions<ExpenseTestDbContext> options)
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
    public DbSet<skestock.Domain.Entities.Expenses.Expense> Expenses => Set<skestock.Domain.Entities.Expenses.Expense>();
    public DbSet<skestock.Domain.Entities.Expenses.ExpenseCategory> ExpenseCategories => Set<skestock.Domain.Entities.Expenses.ExpenseCategory>();
    public DbSet<GoodsReceipt> GoodsReceipts => Set<GoodsReceipt>();
    public DbSet<GoodsReceiptImport> GoodsReceiptImports => Set<GoodsReceiptImport>();
    public DbSet<GoodsReceiptImportLine> GoodsReceiptImportLines => Set<GoodsReceiptImportLine>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<FileMetadata> FileMetadata => Set<FileMetadata>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<ProcessedMessage> ProcessedMessages => Set<ProcessedMessage>();
    public DbSet<OrderList> OrderLists => Set<OrderList>();
    public DbSet<OrderListLine> OrderListLines => Set<OrderListLine>();
    public DbSet<SupplyList> SupplyLists => Set<SupplyList>();
    public DbSet<SupplyListLine> SupplyListLines => Set<SupplyListLine>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Ignore<DailyItemConsumption>();

        builder.Ignore<OrderList>();
        builder.Ignore<OrderListLine>();


        builder.Ignore<CategoryImportBatch>();
        builder.Ignore<CategoryImportBatchFile>();

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
            b.HasOne(c => c.CreatedBy).WithMany().HasForeignKey(c => c.CreatedById);
            b.HasOne(c => c.LastModifiedBy).WithMany().HasForeignKey(c => c.LastModifiedById);
            b.Ignore(c => c.Balances);
            b.Ignore(c => c.BatchesReceived);
            b.Ignore(c => c.Transactions);
            b.Ignore(c => c.GoodsReceipts);
        });

        builder.Ignore<GoodsReceiptImport>();

        builder.Entity<skestock.Domain.Entities.Expenses.ExpenseCategory>(b =>
        {
            b.Ignore(c => c.CreatedBy);
            b.Ignore(c => c.LastModifiedBy);
        });

        builder.Entity<skestock.Domain.Entities.Expenses.Expense>(b =>
        {
            b.HasOne(e => e.SchoolClass).WithMany().HasForeignKey(e => e.SchoolClassId);
            b.HasOne(e => e.FileMetadata).WithMany().HasForeignKey(e => e.FileMetadataId);
            b.HasOne(e => e.ExpenseCategory).WithMany().HasForeignKey(e => e.ExpenseCategoryId);
            b.HasOne(e => e.UploadedByUser).WithMany().HasForeignKey(e => e.UploadedByUserId).HasPrincipalKey(u => u.IdentityId);
            b.HasOne(e => e.CreatedBy).WithMany().HasForeignKey(e => e.CreatedById).HasPrincipalKey(u => u.IdentityId);
            b.HasOne(e => e.LastModifiedBy).WithMany().HasForeignKey(e => e.LastModifiedById).HasPrincipalKey(u => u.IdentityId);
            b.HasQueryFilter(e => e.DeletedDate == null);
        });

        builder.Ignore<GoodsReceiptImportLine>();
        builder.Ignore<GoodsReceipt>();
        builder.Ignore<StockBatch>();
        builder.Ignore<StockTransaction>();
        builder.Ignore<ClassBalance>();
        builder.Ignore<Item>();
        builder.Ignore<Location>();
        builder.Ignore<Category>();
    }
}

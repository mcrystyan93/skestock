using Microsoft.EntityFrameworkCore;
using skestock.Application.Common.Interfaces;
using skestock.Domain.Entities;
using skestock.Domain.Queues;

namespace skestock.Application.UnitTests.Features.StockBatches.Queries.GetAllStockBatches;

/// <summary>
/// Minimal <see cref="IApplicationDbContext"/> implementation for StockBatch handler/validator
/// tests. Maps <see cref="StockBatch"/> and the <see cref="Item"/>/<see cref="Location"/>/
/// <see cref="SchoolClass"/>/<see cref="GoodsReceipt"/>/<see cref="Category"/> entities it
/// references, plus <see cref="UserProfile"/> for the various CreatedBy/LastModifiedBy
/// navigations. Mirrors GoodsReceiptTestDbContext and avoids EF's "ambiguous one-to-one
/// relationship" error the full production model would trigger for UserProfile.
/// </summary>
public class StockBatchTestDbContext(DbContextOptions<StockBatchTestDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<ClassBalance> ClassBalances => Set<ClassBalance>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<SchoolClass> SchoolClasses => Set<SchoolClass>();
    public DbSet<StockBatch> StockBatches => Set<StockBatch>();
    public DbSet<StockTransaction> StockTransactions => Set<StockTransaction>();
    public DbSet<GoodsReceipt> GoodsReceipts => Set<GoodsReceipt>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<FileMetadata> FileMetadata => Set<FileMetadata>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public DbSet<GoodsReceiptImport> GoodsReceiptImports => Set<GoodsReceiptImport>();
    public DbSet<GoodsReceiptImportLine> GoodsReceiptImportLines => Set<GoodsReceiptImportLine>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Ignore<GoodsReceiptImport>();
        builder.Ignore<GoodsReceiptImportLine>();

        builder.Ignore<FileMetadata>();

        builder.Entity<UserProfile>(b =>
        {
            b.Ignore(u => u.CreatedBy);
            b.Ignore(u => u.LastModifiedBy);
            b.Ignore(u => u.Transactions);
            b.Ignore(u => u.GoodsReceipts);
        });

        builder.Entity<Category>(b =>
        {
            b.HasOne(c => c.CreatedBy).WithMany().HasForeignKey(c => c.CreatedById);
            b.HasOne(c => c.LastModifiedBy).WithMany().HasForeignKey(c => c.LastModifiedById);
        });

        builder.Entity<Item>(b =>
        {
            b.HasOne(i => i.CreatedBy).WithMany().HasForeignKey(i => i.CreatedById);
            b.HasOne(i => i.LastModifiedBy).WithMany().HasForeignKey(i => i.LastModifiedById);
            b.HasOne(i => i.Category).WithMany(c => c.Items).HasForeignKey(i => i.CategoryId);
            b.Ignore(i => i.ClassBalances);
        });

        builder.Entity<Location>(b =>
        {
            b.HasOne(l => l.CreatedBy).WithMany().HasForeignKey(l => l.CreatedById);
            b.HasOne(l => l.LastModifiedBy).WithMany().HasForeignKey(l => l.LastModifiedById);
            b.Ignore(l => l.ParentLocation);
            b.Ignore(l => l.ChildLocations);
            b.Ignore(l => l.ClassBalances);
        });

        builder.Entity<SchoolClass>(b =>
        {
            b.HasOne(c => c.CreatedBy).WithMany().HasForeignKey(c => c.CreatedById);
            b.HasOne(c => c.LastModifiedBy).WithMany().HasForeignKey(c => c.LastModifiedById);
            b.Ignore(c => c.Balances);
        });

        builder.Entity<GoodsReceipt>(b =>
        {
            b.HasOne(r => r.Class).WithMany(c => c.GoodsReceipts).HasForeignKey(r => r.ClassId);
            b.HasOne(r => r.CreatedBy).WithMany(u => u.GoodsReceipts).HasForeignKey(r => r.CreatedById);
            b.HasOne(r => r.LastModifiedBy).WithMany().HasForeignKey(r => r.LastModifiedById);
        });

        builder.Entity<StockBatch>(b =>
        {
            b.HasOne(sb => sb.Item).WithMany(i => i.Batches).HasForeignKey(sb => sb.ItemId);
            b.HasOne(sb => sb.Location).WithMany(l => l.Batches).HasForeignKey(sb => sb.LocationId);
            b.HasOne(sb => sb.ReceivedClass).WithMany(c => c.BatchesReceived).HasForeignKey(sb => sb.ReceivedClassId);
            b.HasOne(sb => sb.GoodsReceipt).WithMany(r => r.Batches).HasForeignKey(sb => sb.GoodsReceiptId);
            b.HasOne(sb => sb.CreatedBy).WithMany().HasForeignKey(sb => sb.CreatedById);
            b.HasOne(sb => sb.LastModifiedBy).WithMany().HasForeignKey(sb => sb.LastModifiedById);
        });

        builder.Entity<StockTransaction>(b =>
        {
            b.HasOne(t => t.Item).WithMany(i => i.Transactions).HasForeignKey(t => t.ItemId);
            b.HasOne(t => t.Location).WithMany(l => l.Transactions).HasForeignKey(t => t.LocationId);
            b.HasOne(t => t.Class).WithMany(c => c.Transactions).HasForeignKey(t => t.ClassId);
            b.HasOne(t => t.Batch).WithMany(sb => sb.Transactions).HasForeignKey(t => t.BatchId);
            b.HasOne(t => t.GoodsReceipt).WithMany(r => r.Transactions).HasForeignKey(t => t.GoodsReceiptId);
            b.HasOne(t => t.User).WithMany(u => u.Transactions).HasForeignKey(t => t.UserId);
            b.HasOne(t => t.CreatedBy).WithMany().HasForeignKey(t => t.CreatedById);
            b.HasOne(t => t.LastModifiedBy).WithMany().HasForeignKey(t => t.LastModifiedById);
        });

        builder.Ignore<ClassBalance>();
    }
}

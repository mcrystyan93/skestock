using Microsoft.EntityFrameworkCore;
using skestock.Application.Common.Interfaces;
using skestock.Domain.Entities;
using skestock.Domain.Queues;

namespace skestock.Application.UnitTests.Features.Categories.Commands.CreateCategoryImport;

/// <summary>
/// Minimal <see cref="IApplicationDbContext"/> implementation for CreateCategoryImport
/// handler/validator tests. Maps <see cref="CategoryImport"/> plus the <see cref="FileMetadata"/>
/// and <see cref="UserProfile"/> entities it references. Entities not touched by this slice are
/// ignored so EF's in-memory model stays unambiguous.
/// </summary>
public class CategoryImportTestDbContext(DbContextOptions<CategoryImportTestDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<CategoryImport> CategoryImports => Set<CategoryImport>();
    public DbSet<ClassBalance> ClassBalances => Set<ClassBalance>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<ItemImport> ItemImports => Set<ItemImport>();
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

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Ignore<ItemImport>();

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

        builder.Entity<CategoryImport>(b =>
        {
            b.HasOne(i => i.FileMetadata).WithMany().HasForeignKey(i => i.FileMetadataId);
            b.HasOne(i => i.UploadedByUser).WithMany().HasForeignKey(i => i.UploadedByUserId).HasPrincipalKey(u => u.IdentityId);
            b.HasOne(i => i.CreatedBy).WithMany().HasForeignKey(i => i.CreatedById).HasPrincipalKey(u => u.IdentityId);
            b.HasOne(i => i.LastModifiedBy).WithMany().HasForeignKey(i => i.LastModifiedById).HasPrincipalKey(u => u.IdentityId);
        });

        builder.Ignore<GoodsReceiptImport>();
        builder.Ignore<GoodsReceiptImportLine>();
        builder.Ignore<GoodsReceipt>();
        builder.Ignore<StockBatch>();
        builder.Ignore<StockTransaction>();
        builder.Ignore<ClassBalance>();
        builder.Ignore<Item>();
        builder.Ignore<Location>();
        builder.Ignore<SchoolClass>();
        builder.Ignore<Category>();
    }
}

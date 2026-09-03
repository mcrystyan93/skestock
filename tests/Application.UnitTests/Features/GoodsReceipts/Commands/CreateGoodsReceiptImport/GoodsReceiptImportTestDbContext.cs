using Microsoft.EntityFrameworkCore;
using skestock.Application.Common.Interfaces;
using skestock.Domain.Entities;
using skestock.Domain.Queues;

namespace skestock.Application.UnitTests.Features.GoodsReceipts.Commands.CreateGoodsReceiptImport;

/// <summary>
/// Minimal <see cref="IApplicationDbContext"/> implementation for CreateGoodsReceiptImport
/// handler/validator tests. Maps <see cref="GoodsReceiptImport"/> plus the
/// <see cref="FileMetadata"/>, <see cref="SchoolClass"/> and <see cref="UserProfile"/> entities
/// it references (unlike GoodsReceiptTestDbContext, which ignores FileMetadata/imports). Entities
/// not touched by this slice are ignored so EF's in-memory model stays unambiguous.
/// </summary>
public class GoodsReceiptImportTestDbContext(DbContextOptions<GoodsReceiptImportTestDbContext> options)
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
    public DbSet<GoodsReceiptImport> GoodsReceiptImports => Set<GoodsReceiptImport>();
    public DbSet<GoodsReceiptImportLine> GoodsReceiptImportLines => Set<GoodsReceiptImportLine>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<FileMetadata> FileMetadata => Set<FileMetadata>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<ProcessedMessage> ProcessedMessages => Set<ProcessedMessage>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

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

        builder.Entity<GoodsReceiptImport>(b =>
        {
            b.HasOne(i => i.Class).WithMany().HasForeignKey(i => i.ClassId);
            b.HasOne(i => i.FileMetadata).WithMany().HasForeignKey(i => i.FileMetadataId);
            b.HasOne(i => i.UploadedByUser).WithMany().HasForeignKey(i => i.UploadedByUserId).HasPrincipalKey(u => u.IdentityId);
            b.HasOne(i => i.CreatedBy).WithMany().HasForeignKey(i => i.CreatedById).HasPrincipalKey(u => u.IdentityId);
            b.HasOne(i => i.LastModifiedBy).WithMany().HasForeignKey(i => i.LastModifiedById).HasPrincipalKey(u => u.IdentityId);
            b.Ignore(i => i.ResultingGoodsReceipt);
            b.Ignore(i => i.Lines);
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

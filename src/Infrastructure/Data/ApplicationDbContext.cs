using System.Reflection;
using Microsoft.AspNetCore.Identity;
using skestock.Application.Common.Interfaces;
using skestock.Domain.Common;
using skestock.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using skestock.Domain.Entities;
using skestock.Domain.Queues;

namespace skestock.Infrastructure.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options), IApplicationDbContext
{

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<CategoryImport> CategoryImports => Set<CategoryImport>();
    public DbSet<ClassBalance> ClassBalances => Set<ClassBalance>();
    public DbSet<FileMetadata> FileMetadata => Set<FileMetadata>();
    public DbSet<Item> Items => Set<Item>();
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

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // Domain keys are generated app-side as GUID v7 by GuidV7ValueGenerator. It runs when an
        // entity is tracked as Added (not at SaveChanges), so the real, monotonic key is present in
        // the change tracker immediately - adding multiple same-type entities before saving no
        // longer collides on Guid.Empty, and keyset-pagination ordering on Id stays stable.
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (!typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
                continue;

            builder.Entity(entityType.ClrType)
                .Property(nameof(BaseEntity.Id))
                .HasValueGenerator<GuidV7ValueGenerator>()
                .ValueGeneratedOnAdd();
        }
    }
}

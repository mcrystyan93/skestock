using System.Reflection;
using Microsoft.AspNetCore.Identity;
using skestock.Application.Common.Interfaces;
using skestock.Domain.Common;
using skestock.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using skestock.Application.Common.Filtering;
using skestock.Domain.Entities;
using skestock.Domain.Entities.Categories;
using skestock.Domain.Entities.Congregations;
using skestock.Domain.Entities.FruitDessert;
using skestock.Domain.Entities.GoodsReceipts;
using skestock.Domain.Entities.Items;
using skestock.Domain.Entities.Locations;
using skestock.Domain.Entities.OrderLists;
using skestock.Domain.Entities.ScheduledJobs;
using skestock.Domain.Entities.SchoolClasses;
using skestock.Domain.Entities.Statistics;
using skestock.Domain.Entities.Stock;
using skestock.Domain.Entities.Storage;
using skestock.Domain.Entities.SupplyLists;
using skestock.Domain.Entities.Users;
using skestock.Domain.Queues;

namespace skestock.Infrastructure.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options),
        IApplicationDbContext,
        IScheduledJobRunDbContext
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Congregation> Congregations => Set<Congregation>();
    public DbSet<FruitDessertPlanEntry> FruitDessertPlanEntries => Set<FruitDessertPlanEntry>();
    public DbSet<FruitDessertMessage> FruitDessertMessages => Set<FruitDessertMessage>();
    public DbSet<MessageTemplate> MessageTemplates => Set<MessageTemplate>();
    public DbSet<CategoryImportBatch> CategoryImportBatches => Set<CategoryImportBatch>();
    public DbSet<CategoryImportBatchFile> CategoryImportBatchFiles => Set<CategoryImportBatchFile>();
    public DbSet<ClassBalance> ClassBalances => Set<ClassBalance>();
    public DbSet<ClassItemStockVisibility> ClassItemStockVisibilities => Set<ClassItemStockVisibility>();
    public DbSet<DailyItemConsumption> DailyItemConsumptions => Set<DailyItemConsumption>();
    public DbSet<ItemPurchaseStatistic> ItemPurchaseStatistics => Set<ItemPurchaseStatistic>();
    public DbSet<FileMetadata> FileMetadata => Set<FileMetadata>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<ItemLocationThreshold> ItemLocationThresholds => Set<ItemLocationThreshold>();
    public DbSet<ItemImportBatch> ItemImportBatches => Set<ItemImportBatch>();
    public DbSet<ItemImportBatchFile> ItemImportBatchFiles => Set<ItemImportBatchFile>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<SchoolClass> SchoolClasses => Set<SchoolClass>();
    public DbSet<SharedClassConfiguration> SharedClassConfigurations => Set<SharedClassConfiguration>();
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
    public DbSet<ScheduledJobRun> ScheduledJobRuns => Set<ScheduledJobRun>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<ProcessedMessage> ProcessedMessages => Set<ProcessedMessage>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.UseCollation(TextSearchCollation.Romanian);

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

        ConfigureTemporalTables(builder);
    }

    // Every table is a SQL Server system-versioned (temporal) table unless excluded here, so new entities
    // get history automatically. Period columns are SQL Server-managed datetime2 in UTC (not DateTimeOffset).
    // Retention (2 years) is applied by raw SQL in the AddTemporalTables migration: EF has no API for it, and
    // a later EF column drop/alter can re-enable versioning without it (TemporalSchemaTests catches that).
    private static readonly HashSet<Type> TemporalExclusions =
    [
        typeof(OutboxMessage), // high-churn operational queue, Payload bloat
        typeof(ProcessedMessage), // idempotency bookkeeping
        typeof(ScheduledJobRun), // operational job log
        typeof(DailyItemConsumption), // derived, rebuilt daily: would double-write the table into history
        typeof(ItemPurchaseStatistic), // derived, rebuilt daily
        typeof(IdentityUserToken<Guid>), // 2FA authenticator keys and recovery codes: no copies in history
    ];

    private static void ConfigureTemporalTables(ModelBuilder builder)
    {
        foreach (var entityType in builder.Model.GetEntityTypes().ToList())
        {
            // JSON-mapped owned types (CategoryIcon) live inside the owner's column, they have no table.
            if (TemporalExclusions.Contains(entityType.ClrType) || entityType.IsMappedToJson())
                continue;

            entityType.SetIsTemporal(true);

            // Who changed it: BaseAuditableEntity already has LastModifiedById. Other tables get a shadow column
            // (no FK on purpose: it would block user deletion and create cycles with the Identity tables).
            // Table-sharing owned types use the owner's column.
            if (entityType.FindProperty(LastModifiedById) is null && !IsTableSharingDependent(entityType))
                entityType.AddProperty(LastModifiedById, typeof(Guid?));
        }
    }

    private static bool IsTableSharingDependent(IMutableEntityType entityType) =>
        entityType.FindOwnership() is { } ownership
        && ownership.PrincipalEntityType.GetTableName() == entityType.GetTableName()
        && ownership.PrincipalEntityType.GetSchema() == entityType.GetSchema();

    public const string LastModifiedById = nameof(BaseAuditableEntity.LastModifiedById);
}

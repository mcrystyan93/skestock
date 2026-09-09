using FluentResults;
using Microsoft.EntityFrameworkCore;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.SchoolClasses.Queries.GetSchoolClassSummary;
using skestock.Domain.Entities;
using skestock.Domain.Queues;
using skestock.Domain.Enums;
using NUnit.Framework;
using Shouldly;

namespace skestock.Application.UnitTests.Features.SchoolClasses.Queries.GetSchoolClassSummary;

/// <summary>
/// Minimal <see cref="IApplicationDbContext"/> implementation for handler tests. See
/// CreateSchoolClassCommandHandlerTests' SchoolClassTestDbContext for rationale - this mirrors it
/// for the GetSchoolClassSummary namespace, but keeps Item/Location/StockBatch/GoodsReceipt mapped
/// (rather than ignored) since the handler needs real data across those entities.
/// </summary>
public class SchoolClassSummaryTestDbContext(DbContextOptions<SchoolClassSummaryTestDbContext> options)
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
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<FileMetadata> FileMetadata => Set<FileMetadata>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<ProcessedMessage> ProcessedMessages => Set<ProcessedMessage>();

    public DbSet<GoodsReceiptImport> GoodsReceiptImports => Set<GoodsReceiptImport>();
    public DbSet<GoodsReceiptImportLine> GoodsReceiptImportLines => Set<GoodsReceiptImportLine>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Ignore<ItemImport>();

        builder.Ignore<CategoryImport>();

        builder.Ignore<GoodsReceiptImportLine>();

        builder.Ignore<FileMetadata>();

        builder.Entity<GoodsReceiptImport>(b =>
        {
            b.HasOne(i => i.Class).WithMany().HasForeignKey(i => i.ClassId);
            b.Ignore(i => i.FileMetadata);
            b.Ignore(i => i.UploadedByUser);
            b.Ignore(i => i.ResultingGoodsReceipt);
            b.Ignore(i => i.Lines);
            b.Ignore(i => i.CreatedBy);
            b.Ignore(i => i.LastModifiedBy);
        });

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
            b.OwnsOne(c => c.Icon);
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
            b.Ignore(c => c.Transactions);
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
            b.Ignore(sb => sb.Transactions);
        });

        builder.Ignore<ClassBalance>();
        builder.Ignore<StockTransaction>();
    }
}

public class GetSchoolClassSummaryHandlerTests
{
    private static SchoolClassSummaryTestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<SchoolClassSummaryTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new SchoolClassSummaryTestDbContext(options);
    }

    private static SchoolClass CreateSchoolClass() => new()
    {
        Name = "Fall 2026",
        StartDate = new DateOnly(2026, 9, 1),
        EndDate = new DateOnly(2026, 12, 1),
        Status = ClassStatus.Active
    };

    private static Category CreateCategory() => new() { Name = "Groceries" };

    private static Location CreateLocation(string name = "Main Storage") => new()
    {
        Name = name,
        Type = "StorageRoom"
    };

    private static Item CreateItem(Category category, int minThreshold, string name = "Rice") => new()
    {
        Name = name,
        Unit = "kg",
        MinThreshold = minThreshold,
        IsPerishable = false,
        Category = category
    };

    private static StockBatch CreateBatch(Item item, Location location, SchoolClass schoolClass, int quantity) => new()
    {
        Item = item,
        Location = location,
        ReceivedClass = schoolClass,
        Quantity = quantity,
        ReceivedDate = new DateOnly(2026, 9, 1),
        UnitPrice = 1m
    };

    private static GoodsReceiptImport CreateImport(SchoolClass schoolClass, GoodsReceiptImportStatus status) => new()
    {
        ClassId = schoolClass.Id,
        Class = null!,
        FileMetadataId = Guid.NewGuid(),
        FileMetadata = null!,
        UploadedByUserId = Guid.NewGuid(),
        UploadedByUser = null!,
        BlobPath = "app-files/import.pdf",
        Status = status
    };

    [Test]
    public async Task Handle_WithNonExistentId_ReturnsFailedResult()
    {
        await using var context = CreateContext();
        var handler = new GetSchoolClassSummaryHandler(context);

        var result = await handler.Handle(new GetSchoolClassSummaryQuery { Id = Guid.NewGuid() }, CancellationToken.None);

        result.IsFailed.ShouldBeTrue();
    }

    [Test]
    public async Task Handle_WithNoGoodsReceiptsOrBatches_ReturnsZeroedSummary()
    {
        await using var context = CreateContext();
        var schoolClass = CreateSchoolClass();
        context.SchoolClasses.Add(schoolClass);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetSchoolClassSummaryHandler(context);
        var result = await handler.Handle(new GetSchoolClassSummaryQuery { Id = schoolClass.Id }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.NoOfGoodsReceipt.ShouldBe(0);
        result.Value.TotalAmount.ShouldBe(0m);
        result.Value.LowStockItemsCount.ShouldBe(0);
        result.Value.ProcessingImportsCount.ShouldBe(0);
        result.Value.PendingReviewImportsCount.ShouldBe(0);
        result.Value.FailedImportsCount.ShouldBe(0);
    }

    [Test]
    public async Task Handle_WithItemBelowThreshold_CountsItAsLowStock()
    {
        await using var context = CreateContext();
        var schoolClass = CreateSchoolClass();
        var category = CreateCategory();
        var location = CreateLocation();
        var item = CreateItem(category, minThreshold: 10);
        context.AddRange(schoolClass, category, location, item);
        await context.SaveChangesAsync(CancellationToken.None);

        context.StockBatches.Add(CreateBatch(item, location, schoolClass, quantity: 5));
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetSchoolClassSummaryHandler(context);
        var result = await handler.Handle(new GetSchoolClassSummaryQuery { Id = schoolClass.Id }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.LowStockItemsCount.ShouldBe(1);
    }

    [Test]
    public async Task Handle_CountsImportsPerStatusScopedToTheClass()
    {
        await using var context = CreateContext();
        var schoolClass = CreateSchoolClass();
        var otherClass = CreateSchoolClass();
        context.AddRange(schoolClass, otherClass);
        await context.SaveChangesAsync(CancellationToken.None);

        context.GoodsReceiptImports.AddRange(
            CreateImport(schoolClass, GoodsReceiptImportStatus.Processing),
            CreateImport(schoolClass, GoodsReceiptImportStatus.Processing),
            CreateImport(schoolClass, GoodsReceiptImportStatus.PendingReview),
            CreateImport(schoolClass, GoodsReceiptImportStatus.Failed),
            CreateImport(schoolClass, GoodsReceiptImportStatus.Confirmed),
            // Belongs to a different class - must not be counted.
            CreateImport(otherClass, GoodsReceiptImportStatus.Processing));
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetSchoolClassSummaryHandler(context);
        var result = await handler.Handle(new GetSchoolClassSummaryQuery { Id = schoolClass.Id }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ProcessingImportsCount.ShouldBe(2);
        result.Value.PendingReviewImportsCount.ShouldBe(1);
        result.Value.FailedImportsCount.ShouldBe(1);
    }

    [Test]
    public async Task Handle_WithItemAtOrAboveThreshold_DoesNotCountAsLowStock()
    {
        await using var context = CreateContext();
        var schoolClass = CreateSchoolClass();
        var category = CreateCategory();
        var location = CreateLocation();
        var item = CreateItem(category, minThreshold: 10);
        context.AddRange(schoolClass, category, location, item);
        await context.SaveChangesAsync(CancellationToken.None);

        context.StockBatches.Add(CreateBatch(item, location, schoolClass, quantity: 10));
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetSchoolClassSummaryHandler(context);
        var result = await handler.Handle(new GetSchoolClassSummaryQuery { Id = schoolClass.Id }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.LowStockItemsCount.ShouldBe(0);
    }

    [Test]
    public async Task Handle_SumsQuantityAcrossLocationsBeforeComparingToThreshold()
    {
        await using var context = CreateContext();
        var schoolClass = CreateSchoolClass();
        var category = CreateCategory();
        var locationA = CreateLocation("Kitchen");
        var locationB = CreateLocation("Storage Room");
        var item = CreateItem(category, minThreshold: 10);
        context.AddRange(schoolClass, category, locationA, locationB, item);
        await context.SaveChangesAsync(CancellationToken.None);

        // 6 + 6 = 12 >= 10 threshold when summed across locations, so it should NOT be low stock,
        // even though each individual location's batch (6) is below the threshold on its own.
        context.StockBatches.AddRange(
            CreateBatch(item, locationA, schoolClass, quantity: 6),
            CreateBatch(item, locationB, schoolClass, quantity: 6));
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetSchoolClassSummaryHandler(context);
        var result = await handler.Handle(new GetSchoolClassSummaryQuery { Id = schoolClass.Id }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.LowStockItemsCount.ShouldBe(0);
    }
}

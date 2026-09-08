using Microsoft.EntityFrameworkCore;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.Locations.Queries.GetDefaultLocation;
using skestock.Domain.Entities;
using skestock.Domain.Queues;
using NUnit.Framework;
using Shouldly;

namespace skestock.Application.UnitTests.Features.Locations.Queries.GetDefaultLocation;

/// <summary>
/// Minimal <see cref="IApplicationDbContext"/> implementation for handler tests. Mirrors
/// GetLocationByIdHandlerTests' LocationTestDbContext: only maps <see cref="Location"/> and the
/// <see cref="UserProfile"/> it references, ignoring everything else the handler never touches.
/// </summary>
public class DefaultLocationTestDbContext(DbContextOptions<DefaultLocationTestDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<CategoryImport> CategoryImports => Set<CategoryImport>();
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
    public DbSet<ProcessedMessage> ProcessedMessages => Set<ProcessedMessage>();

    public DbSet<GoodsReceiptImport> GoodsReceiptImports => Set<GoodsReceiptImport>();
    public DbSet<GoodsReceiptImportLine> GoodsReceiptImportLines => Set<GoodsReceiptImportLine>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Ignore<CategoryImport>();

        builder.Ignore<GoodsReceiptImport>();
        builder.Ignore<GoodsReceiptImportLine>();

        builder.Ignore<FileMetadata>();

        builder.Entity<UserProfile>(b =>
        {
            b.Ignore(u => u.CreatedBy);
            b.Ignore(u => u.LastModifiedBy);
        });

        builder.Entity<Location>(b =>
        {
            b.HasOne(l => l.CreatedBy).WithMany().HasForeignKey(l => l.CreatedById);
            b.HasOne(l => l.LastModifiedBy).WithMany().HasForeignKey(l => l.LastModifiedById);
            b.HasOne(l => l.ParentLocation).WithMany(l => l.ChildLocations).HasForeignKey(l => l.ParentLocationId);
            b.Ignore(l => l.Batches);
            b.Ignore(l => l.Transactions);
            b.Ignore(l => l.ClassBalances);
        });

        builder.Ignore<Category>();
        builder.Ignore<ClassBalance>();
        builder.Ignore<Item>();
        builder.Ignore<SchoolClass>();
        builder.Ignore<StockBatch>();
        builder.Ignore<StockTransaction>();
        builder.Ignore<GoodsReceipt>();
    }
}

public class GetDefaultLocationHandlerTests
{
    private static DefaultLocationTestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<DefaultLocationTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new DefaultLocationTestDbContext(options);
    }

    [Test]
    public async Task Handle_WithDefaultLocation_ReturnsDefaultDto()
    {
        await using var context = CreateContext();
        context.Locations.Add(new Location { Name = "Frigider", Type = "Bucatarie", IsDefault = false });
        var defaultLocation = new Location { Name = "Camara", Type = "Bucatarie", IsDefault = true };
        context.Locations.Add(defaultLocation);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetDefaultLocationHandler(context);
        var result = await handler.Handle(new GetDefaultLocationQuery(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Id.ShouldBe(defaultLocation.Id);
        result.Value.Name.ShouldBe("Camara");
        result.Value.IsDefault.ShouldBeTrue();
    }

    [Test]
    public async Task Handle_WithNoDefaultLocation_ReturnsSuccessWithNull()
    {
        await using var context = CreateContext();
        context.Locations.Add(new Location { Name = "Frigider", Type = "Bucatarie", IsDefault = false });
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetDefaultLocationHandler(context);
        var result = await handler.Handle(new GetDefaultLocationQuery(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeNull();
    }

    [Test]
    public async Task Handle_WithNoLocationsAtAll_ReturnsSuccessWithNull()
    {
        await using var context = CreateContext();

        var handler = new GetDefaultLocationHandler(context);
        var result = await handler.Handle(new GetDefaultLocationQuery(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeNull();
    }
}

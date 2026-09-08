using FluentResults;
using Microsoft.EntityFrameworkCore;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.Locations.Queries.GetLocationById;
using skestock.Domain.Entities;
using skestock.Domain.Queues;
using NUnit.Framework;
using Shouldly;

namespace skestock.Application.UnitTests.Features.Locations.Queries.GetLocationById;

/// <summary>
/// Minimal <see cref="IApplicationDbContext"/> implementation for handler tests. Only maps
/// <see cref="Location"/> (self-referencing ParentLocation) and the <see cref="UserProfile"/> it
/// references for CreatedBy/LastModifiedBy; all other DbSets required by the interface are left
/// unmapped/ignored since <see cref="GetLocationByIdHandler"/> never touches them. Mirrors
/// GetCategoryByIdHandlerTests' CategoryTestDbContext and avoids EF's "ambiguous one-to-one
/// relationship" error the full production model would trigger for UserProfile.
/// </summary>
public class LocationTestDbContext(DbContextOptions<LocationTestDbContext> options)
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

public class GetLocationByIdHandlerTests
{
    private static LocationTestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<LocationTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new LocationTestDbContext(options);
    }

    [Test]
    public async Task Handle_WithExistingId_ReturnsMatchingLocationDto()
    {
        await using var context = CreateContext();
        var location = new Location { Name = "Kitchen", Type = "Kitchen" };
        context.Locations.Add(location);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetLocationByIdHandler(context);
        var result = await handler.Handle(new GetLocationByIdQuery { Id = location.Id }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Id.ShouldBe(location.Id);
        result.Value.Name.ShouldBe("Kitchen");
        result.Value.Type.ShouldBe("Kitchen");
    }

    [Test]
    public async Task Handle_WithParentLocation_ReturnsParentLocationNameInDto()
    {
        await using var context = CreateContext();
        var parent = new Location { Name = "Main Building", Type = "Building" };
        context.Locations.Add(parent);
        await context.SaveChangesAsync(CancellationToken.None);
        var location = new Location { Name = "Room 101", Type = "Room", ParentLocationId = parent.Id };
        context.Locations.Add(location);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetLocationByIdHandler(context);
        var result = await handler.Handle(new GetLocationByIdQuery { Id = location.Id }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ParentLocationId.ShouldBe(parent.Id);
        result.Value.ParentLocationName.ShouldBe("Main Building");
    }

    [Test]
    public async Task Handle_WithNonExistentId_ReturnsFailedResult()
    {
        await using var context = CreateContext();
        var handler = new GetLocationByIdHandler(context);

        var result = await handler.Handle(new GetLocationByIdQuery { Id = Guid.NewGuid() }, CancellationToken.None);

        result.IsFailed.ShouldBeTrue();
    }
}

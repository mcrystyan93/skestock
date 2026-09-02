using FluentResults;
using Microsoft.EntityFrameworkCore;
using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.Locations.Commands.UpdateLocation;
using skestock.Domain.Entities;
using skestock.Domain.Queues;
using NUnit.Framework;
using Shouldly;

namespace skestock.Application.UnitTests.Features.Locations.Commands.UpdateLocation;

/// <summary>
/// Minimal <see cref="IApplicationDbContext"/> implementation for handler/validator tests. Only
/// maps <see cref="Location"/> (self-referencing ParentLocation) and the <see cref="UserProfile"/>
/// it references for CreatedBy/LastModifiedBy; all other DbSets required by the interface are
/// left unmapped/ignored since <see cref="UpdateLocationCommandHandler"/> never touches them.
/// Mirrors CategoryTestDbContext/CreateLocation's LocationTestDbContext and avoids EF's "ambiguous
/// one-to-one relationship" error the full production model would trigger for UserProfile.
/// </summary>
public class LocationTestDbContext(DbContextOptions<LocationTestDbContext> options)
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

public class UpdateLocationCommandHandlerTests
{
    private static LocationTestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<LocationTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new LocationTestDbContext(options);
    }

    [Test]
    public async Task Handle_WithExistingLocation_UpdatesNameAndTypeAndReturnsDto()
    {
        await using var context = CreateContext();
        var location = new Location { Name = "Old Name", Type = "OldType" };
        context.Locations.Add(location);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new UpdateLocationCommandHandler(context);
        var result = await handler.Handle(
            new UpdateLocationCommand { Id = location.Id, Name = "New Name", Type = "NewType" },
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Id.ShouldBe(location.Id);
        result.Value.Name.ShouldBe("New Name");
        result.Value.Type.ShouldBe("NewType");

        var persisted = await context.Locations.SingleAsync(l => l.Id == location.Id, CancellationToken.None);
        persisted.Name.ShouldBe("New Name");
        persisted.Type.ShouldBe("NewType");
    }

    [Test]
    public async Task Handle_WithNameAndTypeContainingWhitespace_TrimsBeforePersisting()
    {
        await using var context = CreateContext();
        var location = new Location { Name = "Old Name", Type = "OldType" };
        context.Locations.Add(location);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new UpdateLocationCommandHandler(context);
        var result = await handler.Handle(
            new UpdateLocationCommand { Id = location.Id, Name = "  Trimmed  ", Type = "  Type  " },
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Name.ShouldBe("Trimmed");
        result.Value.Type.ShouldBe("Type");
    }

    [Test]
    public async Task Handle_WithParentLocationId_UpdatesRelationshipAndReturnsParentName()
    {
        await using var context = CreateContext();
        var parent = new Location { Name = "Main Building", Type = "Building" };
        var location = new Location { Name = "Room 101", Type = "Room" };
        context.Locations.AddRange(parent, location);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new UpdateLocationCommandHandler(context);
        var result = await handler.Handle(
            new UpdateLocationCommand { Id = location.Id, Name = "Room 101", Type = "Room", ParentLocationId = parent.Id },
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ParentLocationId.ShouldBe(parent.Id);
        result.Value.ParentLocationName.ShouldBe("Main Building");
    }

    [Test]
    public async Task Handle_WithParentLocationIdRemoved_ClearsRelationship()
    {
        await using var context = CreateContext();
        var parent = new Location { Name = "Main Building", Type = "Building" };
        context.Locations.Add(parent);
        await context.SaveChangesAsync(CancellationToken.None);
        var location = new Location { Name = "Room 101", Type = "Room", ParentLocationId = parent.Id };
        context.Locations.Add(location);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new UpdateLocationCommandHandler(context);
        var result = await handler.Handle(
            new UpdateLocationCommand { Id = location.Id, Name = "Room 101", Type = "Room", ParentLocationId = null },
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ParentLocationId.ShouldBeNull();
        result.Value.ParentLocationName.ShouldBeNull();
    }

    [Test]
    public async Task Handle_WithNonExistentId_ReturnsFailedResult()
    {
        await using var context = CreateContext();
        var handler = new UpdateLocationCommandHandler(context);

        var result = await handler.Handle(
            new UpdateLocationCommand { Id = Guid.NewGuid(), Name = "Anything", Type = "AnyType" },
            CancellationToken.None);

        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(e => e is LocationErrors.LocationNotFound);
    }
}

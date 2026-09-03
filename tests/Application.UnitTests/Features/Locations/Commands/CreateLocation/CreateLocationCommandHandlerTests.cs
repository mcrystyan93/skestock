using FluentResults;
using Microsoft.EntityFrameworkCore;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.Locations.Commands.CreateLocation;
using skestock.Domain.Entities;
using skestock.Domain.Queues;
using NUnit.Framework;
using Shouldly;

namespace skestock.Application.UnitTests.Features.Locations.Commands.CreateLocation;

/// <summary>
/// Minimal <see cref="IApplicationDbContext"/> implementation for handler/validator tests. Only
/// maps <see cref="Location"/> (self-referencing ParentLocation) and the <see cref="UserProfile"/>
/// it references for CreatedBy/LastModifiedBy; all other DbSets required by the interface are
/// left unmapped/ignored since <see cref="CreateLocationCommandHandler"/> never touches them.
/// Mirrors CategoryTestDbContext and avoids EF's "ambiguous one-to-one relationship" error the
/// full production model would trigger for UserProfile.
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
    public DbSet<ProcessedMessage> ProcessedMessages => Set<ProcessedMessage>();

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

public class CreateLocationCommandHandlerTests
{
    private static LocationTestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<LocationTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new LocationTestDbContext(options);
    }

    [Test]
    public async Task Handle_WithValidNameAndType_PersistsLocationAndReturnsDto()
    {
        await using var context = CreateContext();
        var handler = new CreateLocationCommandHandler(context);

        var result = await handler.Handle(
            new CreateLocationCommand { Name = "Main Kitchen", Type = "Kitchen" },
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Name.ShouldBe("Main Kitchen");
        result.Value.Type.ShouldBe("Kitchen");
        result.Value.ParentLocationId.ShouldBeNull();
        result.Value.Id.ShouldNotBe(Guid.Empty);

        var persisted = await context.Locations.SingleAsync(CancellationToken.None);
        persisted.Name.ShouldBe("Main Kitchen");
        persisted.Id.ShouldBe(result.Value.Id);
    }

    [Test]
    public async Task Handle_WithNameAndTypeContainingLeadingOrTrailingWhitespace_TrimsBeforePersisting()
    {
        await using var context = CreateContext();
        var handler = new CreateLocationCommandHandler(context);

        var result = await handler.Handle(
            new CreateLocationCommand { Name = "  Storage Room  ", Type = "  StorageRoom  " },
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Name.ShouldBe("Storage Room");
        result.Value.Type.ShouldBe("StorageRoom");

        var persisted = await context.Locations.SingleAsync(CancellationToken.None);
        persisted.Name.ShouldBe("Storage Room");
        persisted.Type.ShouldBe("StorageRoom");
    }

    [Test]
    public async Task Handle_WithParentLocationId_PersistsRelationshipAndReturnsParentName()
    {
        await using var context = CreateContext();
        var parent = new Location { Name = "Main Building", Type = "Building" };
        context.Locations.Add(parent);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new CreateLocationCommandHandler(context);
        var result = await handler.Handle(
            new CreateLocationCommand { Name = "Room 101", Type = "Room", ParentLocationId = parent.Id },
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ParentLocationId.ShouldBe(parent.Id);
        result.Value.ParentLocationName.ShouldBe("Main Building");

        var persisted = await context.Locations.SingleAsync(l => l.Name == "Room 101", CancellationToken.None);
        persisted.ParentLocationId.ShouldBe(parent.Id);
    }

    [Test]
    public async Task Handle_CalledTwice_PersistsTwoIndependentLocations()
    {
        await using var context = CreateContext();
        var handler = new CreateLocationCommandHandler(context);

        var first = await handler.Handle(new CreateLocationCommand { Name = "Gym", Type = "Gym" }, CancellationToken.None);
        var second = await handler.Handle(new CreateLocationCommand { Name = "Library", Type = "Library" }, CancellationToken.None);

        first.Value.Id.ShouldNotBe(second.Value.Id);
        (await context.Locations.CountAsync(CancellationToken.None)).ShouldBe(2);
    }
}

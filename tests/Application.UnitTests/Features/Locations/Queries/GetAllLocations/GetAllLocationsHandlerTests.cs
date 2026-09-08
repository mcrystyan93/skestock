using FluentResults;
using Microsoft.EntityFrameworkCore;
using skestock.Application.Common.Filtering;
using skestock.Application.Common.Interfaces;
using skestock.Application.Common.Models;
using skestock.Application.Features.Locations.Models;
using skestock.Application.Features.Locations.Queries.GetAllLocations;
using skestock.Domain.Entities;
using skestock.Domain.Queues;
using NUnit.Framework;
using Shouldly;

namespace skestock.Application.UnitTests.Features.Locations.Queries.GetAllLocations;

/// <summary>
/// Minimal <see cref="IApplicationDbContext"/> implementation for handler tests. Only maps
/// <see cref="Location"/> (self-referencing ParentLocation) and the <see cref="UserProfile"/> it
/// references for CreatedBy/LastModifiedBy; all other DbSets required by the interface are left
/// unmapped/ignored since <see cref="GetAllLocationsHandler"/> never touches them. Mirrors
/// GetAllCategoriesHandlerTests' CategoryTestDbContext and avoids EF's "ambiguous one-to-one
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

public class GetAllLocationsHandlerTests
{
    private static async Task<LocationTestDbContext> SeedAsync(int count, Func<int, Location>? factory = null)
    {
        var options = new DbContextOptionsBuilder<LocationTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new LocationTestDbContext(options);
        var baseline = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

        for (var i = 0; i < count; i++)
        {
            var location = factory?.Invoke(i) ?? new Location
            {
                Name = $"Location{i:D2}",
                Type = "Room",
                CreatedDate = baseline.AddMinutes(i),
                LastModifiedDate = baseline.AddMinutes(i)
            };
            context.Locations.Add(location);
        }

        await context.SaveChangesAsync(CancellationToken.None);
        return context;
    }

    private static GetAllLocationsHandler CreateHandler(IApplicationDbContext context) => new(context);

    [Test]
    public async Task Handle_WithNoCursor_ReturnsFirstPageOrderedByDefaultSort()
    {
        await using var context = await SeedAsync(5);
        var handler = CreateHandler(context);

        var result = await handler.Handle(new GetAllLocationsQuery { PageSize = 10 }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        var page = result.Value;
        page.Data.Count().ShouldBe(5);
        page.HasNextPage.ShouldBeFalse();
        page.Data.Select(l => l.Name).ShouldBe(["Location04", "Location03", "Location02", "Location01", "Location00"]);
    }

    [Test]
    public async Task Handle_WithPageSizeSmallerThanTotal_SetsHasNextPageAndReturnsCursor()
    {
        await using var context = await SeedAsync(5);
        var handler = CreateHandler(context);

        var result = await handler.Handle(new GetAllLocationsQuery { PageSize = 2 }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Data.Count().ShouldBe(2);
        result.Value.HasNextPage.ShouldBeTrue();
        result.Value.NextCursor.ShouldNotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Handle_WithEmptyDataSet_ReturnsEmptyPageWithNoCursor()
    {
        await using var context = await SeedAsync(0);
        var handler = CreateHandler(context);

        var result = await handler.Handle(new GetAllLocationsQuery(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Data.ShouldBeEmpty();
        result.Value.HasNextPage.ShouldBeFalse();
        result.Value.NextCursor.ShouldBeNull();
    }

    [Test]
    public async Task Handle_WithNameSortAscending_ReturnsItemsInAlphabeticalOrder()
    {
        await using var context = await SeedAsync(3, i => new Location
        {
            Name = i switch { 0 => "Charlie", 1 => "Alpha", _ => "Bravo" },
            Type = "Room",
            CreatedDate = new DateTimeOffset(2024, 1, 1 + i, 0, 0, 0, TimeSpan.Zero),
            LastModifiedDate = new DateTimeOffset(2024, 1, 1 + i, 0, 0, 0, TimeSpan.Zero)
        });
        var handler = CreateHandler(context);

        var query = new GetAllLocationsQuery
        {
            Sort = [new PaginationSort { Key = "name", Value = "ascend" }]
        };
        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Data.Select(l => l.Name).ShouldBe(["Alpha", "Bravo", "Charlie"]);
        result.Value.Sort.ShouldContain(s => s.Key == "name" && s.Value == "ascend");
    }

    [Test]
    public async Task Handle_PagingThroughCursors_ReturnsAllItemsExactlyOnceInOrder()
    {
        await using var context = await SeedAsync(11);
        var handler = CreateHandler(context);

        var collected = new List<LocationDto>();
        string? cursor = null;
        var safetyCounter = 0;

        while (true)
        {
            safetyCounter++.ShouldBeLessThan(20);

            var result = await handler.Handle(
                new GetAllLocationsQuery { PageSize = 3, Cursor = cursor },
                CancellationToken.None);

            result.IsSuccess.ShouldBeTrue();
            var page = result.Value;
            collected.AddRange(page.Data);

            if (!page.HasNextPage)
                break;

            cursor = page.NextCursor;
        }

        collected.Select(l => l.Id).Distinct().Count().ShouldBe(11);
        collected.Select(l => l.Name).ShouldBe(Enumerable.Range(0, 11).Reverse().Select(i => $"Location{i:D2}"));
    }

    [Test]
    public async Task Handle_WithSearchTerm_FiltersByNameContains()
    {
        await using var context = await SeedAsync(3, i => new Location
        {
            Name = i switch { 0 => "Widgets", 1 => "Gadgets", _ => "Gizmos" },
            Type = "Storage",
            CreatedDate = new DateTimeOffset(2024, 1, 1 + i, 0, 0, 0, TimeSpan.Zero),
            LastModifiedDate = new DateTimeOffset(2024, 1, 1 + i, 0, 0, 0, TimeSpan.Zero)
        });
        var handler = CreateHandler(context);

        var result = await handler.Handle(new GetAllLocationsQuery { SearchTerm = "get" }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Data.Select(l => l.Name).ShouldBe(["Gadgets", "Widgets"]);
    }

    [Test]
    public async Task Handle_WithColumnFilterEquals_ReturnsOnlyMatchingRow()
    {
        await using var context = await SeedAsync(5);
        var handler = CreateHandler(context);
        var targetId = context.Locations.Single(l => l.Name == "Location02").Id;

        var query = new GetAllLocationsQuery
        {
            Filters = [new ColumnFilter("id", FilterOperator.Equals, targetId)]
        };
        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Data.Count().ShouldBe(1);
        result.Value.Data.Single().Name.ShouldBe("Location02");
    }

    [Test]
    public async Task Handle_WithColumnFilterOnType_ReturnsOnlyMatchingRows()
    {
        await using var context = await SeedAsync(3, i => new Location
        {
            Name = $"Location{i:D2}",
            Type = i == 0 ? "Kitchen" : "StorageRoom",
            CreatedDate = new DateTimeOffset(2024, 1, 1 + i, 0, 0, 0, TimeSpan.Zero),
            LastModifiedDate = new DateTimeOffset(2024, 1, 1 + i, 0, 0, 0, TimeSpan.Zero)
        });
        var handler = CreateHandler(context);

        var query = new GetAllLocationsQuery
        {
            Filters = [new ColumnFilter("type", FilterOperator.Equals, "StorageRoom")]
        };
        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Data.Count().ShouldBe(2);
        result.Value.Data.ShouldAllBe(l => l.Type == "StorageRoom");
    }

    [Test]
    public async Task Handle_WithParentLocation_ReturnsParentLocationNameInDto()
    {
        await using var context = CreateContextForParentTest();
        var handler = CreateHandler(context);

        var result = await handler.Handle(new GetAllLocationsQuery { SearchTerm = "Room 101" }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        var dto = result.Value.Data.Single();
        dto.ParentLocationName.ShouldBe("Main Building");
    }

    private static LocationTestDbContext CreateContextForParentTest()
    {
        var options = new DbContextOptionsBuilder<LocationTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new LocationTestDbContext(options);
        var parent = new Location { Name = "Main Building", Type = "Building" };
        context.Locations.Add(parent);
        context.SaveChanges();
        context.Locations.Add(new Location { Name = "Room 101", Type = "Room", ParentLocationId = parent.Id });
        context.SaveChanges();
        return context;
    }

    [Test]
    public async Task Handle_WithMalformedCursor_IsIgnoredAndReturnsFirstPage()
    {
        await using var context = await SeedAsync(3);
        var handler = CreateHandler(context);

        var result = await handler.Handle(
            new GetAllLocationsQuery { Cursor = "not-a-valid-cursor-token" },
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Data.Count().ShouldBe(3);
    }
}

using FluentResults;
using Microsoft.EntityFrameworkCore;
using skestock.Application.Common.Filtering;
using skestock.Application.Common.Interfaces;
using skestock.Application.Common.Models;
using skestock.Application.Features.SchoolClasses.Models;
using skestock.Application.Features.SchoolClasses.Queries.GetAllSchoolClasses;
using skestock.Domain.Entities;
using skestock.Domain.Queues;
using skestock.Domain.Enums;
using NUnit.Framework;
using Shouldly;

namespace skestock.Application.UnitTests.Features.SchoolClasses.Queries.GetAllSchoolClasses;

/// <summary>
/// Minimal <see cref="IApplicationDbContext"/> implementation for handler tests. See
/// CreateSchoolClassCommandHandlerTests' SchoolClassTestDbContext for rationale - this mirrors it
/// for the GetAllSchoolClasses namespace.
/// </summary>
public class SchoolClassTestDbContext(DbContextOptions<SchoolClassTestDbContext> options)
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

        builder.Entity<SchoolClass>(b =>
        {
            b.HasOne(c => c.CreatedBy).WithMany().HasForeignKey(c => c.CreatedById);
            b.HasOne(c => c.LastModifiedBy).WithMany().HasForeignKey(c => c.LastModifiedById);
            b.Ignore(c => c.BatchesReceived);
            b.Ignore(c => c.Transactions);
            b.Ignore(c => c.Balances);
        });

        builder.Ignore<Category>();
        builder.Ignore<ClassBalance>();
        builder.Ignore<Item>();
        builder.Ignore<Location>();
        builder.Ignore<StockBatch>();
        builder.Ignore<StockTransaction>();
        builder.Ignore<GoodsReceipt>();
    }
}

public class GetAllSchoolClassesHandlerTests
{
    private static async Task<SchoolClassTestDbContext> SeedAsync(int count, Func<int, SchoolClass>? factory = null)
    {
        var options = new DbContextOptionsBuilder<SchoolClassTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new SchoolClassTestDbContext(options);
        var baseline = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

        for (var i = 0; i < count; i++)
        {
            var schoolClass = factory?.Invoke(i) ?? new SchoolClass
            {
                Name = $"Class{i:D2}",
                StartDate = new DateOnly(2026, 1, 1).AddDays(i),
                EndDate = new DateOnly(2026, 6, 1).AddDays(i),
                CreatedDate = baseline.AddMinutes(i),
                LastModifiedDate = baseline.AddMinutes(i)
            };
            context.SchoolClasses.Add(schoolClass);
        }

        await context.SaveChangesAsync(CancellationToken.None);
        return context;
    }

    private static GetAllSchoolClassesHandler CreateHandler(IApplicationDbContext context) => new(context);

    [Test]
    public async Task Handle_WithNoCursor_ReturnsFirstPageOrderedByDefaultSort()
    {
        await using var context = await SeedAsync(5);
        var handler = CreateHandler(context);

        var result = await handler.Handle(new GetAllSchoolClassesQuery { PageSize = 10 }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        var page = result.Value;
        page.Data.Count().ShouldBe(5);
        page.HasNextPage.ShouldBeFalse();
        page.Data.Select(c => c.Name).ShouldBe(["Class04", "Class03", "Class02", "Class01", "Class00"]);
    }

    [Test]
    public async Task Handle_WithPageSizeSmallerThanTotal_SetsHasNextPageAndReturnsCursor()
    {
        await using var context = await SeedAsync(5);
        var handler = CreateHandler(context);

        var result = await handler.Handle(new GetAllSchoolClassesQuery { PageSize = 2 }, CancellationToken.None);

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

        var result = await handler.Handle(new GetAllSchoolClassesQuery(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Data.ShouldBeEmpty();
        result.Value.HasNextPage.ShouldBeFalse();
        result.Value.NextCursor.ShouldBeNull();
    }

    [Test]
    public async Task Handle_WithNameSortAscending_ReturnsItemsInAlphabeticalOrder()
    {
        await using var context = await SeedAsync(3, i => new SchoolClass
        {
            Name = i switch { 0 => "Charlie", 1 => "Alpha", _ => "Bravo" },
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 6, 1),
            CreatedDate = new DateTimeOffset(2024, 1, 1 + i, 0, 0, 0, TimeSpan.Zero),
            LastModifiedDate = new DateTimeOffset(2024, 1, 1 + i, 0, 0, 0, TimeSpan.Zero)
        });
        var handler = CreateHandler(context);

        var query = new GetAllSchoolClassesQuery
        {
            Sort = [new PaginationSort { Key = "name", Value = "ascend" }]
        };
        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Data.Select(c => c.Name).ShouldBe(["Alpha", "Bravo", "Charlie"]);
        result.Value.Sort.ShouldContain(s => s.Key == "name" && s.Value == "ascend");
    }

    [Test]
    public async Task Handle_PagingThroughCursors_ReturnsAllItemsExactlyOnceInOrder()
    {
        await using var context = await SeedAsync(11);
        var handler = CreateHandler(context);

        var collected = new List<SchoolClassDto>();
        string? cursor = null;
        var safetyCounter = 0;

        while (true)
        {
            safetyCounter++.ShouldBeLessThan(20);

            var result = await handler.Handle(
                new GetAllSchoolClassesQuery { PageSize = 3, Cursor = cursor },
                CancellationToken.None);

            result.IsSuccess.ShouldBeTrue();
            var page = result.Value;
            collected.AddRange(page.Data);

            if (!page.HasNextPage)
                break;

            cursor = page.NextCursor;
        }

        collected.Select(c => c.Id).Distinct().Count().ShouldBe(11);
        collected.Select(c => c.Name).ShouldBe(Enumerable.Range(0, 11).Reverse().Select(i => $"Class{i:D2}"));
    }

    [Test]
    public async Task Handle_WithSearchTerm_FiltersByNameContains()
    {
        await using var context = await SeedAsync(3, i => new SchoolClass
        {
            Name = i switch { 0 => "Widgets", 1 => "Gadgets", _ => "Gizmos" },
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 6, 1),
            CreatedDate = new DateTimeOffset(2024, 1, 1 + i, 0, 0, 0, TimeSpan.Zero),
            LastModifiedDate = new DateTimeOffset(2024, 1, 1 + i, 0, 0, 0, TimeSpan.Zero)
        });
        var handler = CreateHandler(context);

        var result = await handler.Handle(new GetAllSchoolClassesQuery { SearchTerm = "get" }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Data.Select(c => c.Name).ShouldBe(["Gadgets", "Widgets"]);
    }

    [Test]
    public async Task Handle_WithColumnFilterEquals_ReturnsOnlyMatchingRow()
    {
        await using var context = await SeedAsync(5);
        var handler = CreateHandler(context);
        var targetId = context.SchoolClasses.Single(c => c.Name == "Class02").Id;

        var query = new GetAllSchoolClassesQuery
        {
            Filters = [new ColumnFilter("id", FilterOperator.Equals, targetId)]
        };
        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Data.Count().ShouldBe(1);
        result.Value.Data.Single().Name.ShouldBe("Class02");
    }

    [Test]
    public async Task Handle_WithColumnFilterOnStatus_ReturnsOnlyMatchingRows()
    {
        await using var context = await SeedAsync(3, i => new SchoolClass
        {
            Name = $"Class{i:D2}",
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 6, 1),
            Status = i == 0 ? ClassStatus.Active : ClassStatus.Upcoming,
            CreatedDate = new DateTimeOffset(2024, 1, 1 + i, 0, 0, 0, TimeSpan.Zero),
            LastModifiedDate = new DateTimeOffset(2024, 1, 1 + i, 0, 0, 0, TimeSpan.Zero)
        });
        var handler = CreateHandler(context);

        var query = new GetAllSchoolClassesQuery
        {
            Filters = [new ColumnFilter("status", FilterOperator.Equals, (int)ClassStatus.Active)]
        };
        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Data.Count().ShouldBe(1);
        result.Value.Data.Single().Name.ShouldBe("Class00");
    }

    [Test]
    public async Task Handle_WithColumnFilterOnStatusAsString_ReturnsOnlyMatchingRows()
    {
        await using var context = await SeedAsync(3, i => new SchoolClass
        {
            Name = $"Class{i:D2}",
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 6, 1),
            Status = i == 1 ? ClassStatus.Closed : ClassStatus.Upcoming,
            CreatedDate = new DateTimeOffset(2024, 1, 1 + i, 0, 0, 0, TimeSpan.Zero),
            LastModifiedDate = new DateTimeOffset(2024, 1, 1 + i, 0, 0, 0, TimeSpan.Zero)
        });
        var handler = CreateHandler(context);

        var query = new GetAllSchoolClassesQuery
        {
            Filters = [new ColumnFilter("status", FilterOperator.Equals, "Closed")]
        };
        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Data.Count().ShouldBe(1);
        result.Value.Data.Single().Name.ShouldBe("Class01");
    }

    [Test]
    public async Task Handle_WithMalformedCursor_IsIgnoredAndReturnsFirstPage()
    {
        await using var context = await SeedAsync(3);
        var handler = CreateHandler(context);

        var result = await handler.Handle(
            new GetAllSchoolClassesQuery { Cursor = "not-a-valid-cursor-token" },
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Data.Count().ShouldBe(3);
    }
}

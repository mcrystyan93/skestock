using FluentResults;
using Microsoft.EntityFrameworkCore;
using skestock.Application.Common.Filtering;
using skestock.Application.Common.Interfaces;
using skestock.Application.Common.Models;
using skestock.Application.Features.Categories.Models;
using skestock.Application.Features.Categories.Queries.GetAllCategories;
using skestock.Domain.Entities;
using skestock.Domain.Queues;
using NUnit.Framework;
using Shouldly;

namespace skestock.Application.UnitTests.Features.Categories.Queries.GetAllCategories;

/// <summary>
/// Minimal <see cref="IApplicationDbContext"/> implementation for handler tests. Only maps
/// <see cref="Category"/> (and the <see cref="UserProfile"/> it references for CreatedBy/LastModifiedBy);
/// all other DbSets required by the interface are left unmapped/ignored since
/// <see cref="GetAllCategoriesHandler"/> never touches them. This keeps the in-memory model minimal
/// and avoids EF's "ambiguous one-to-one relationship" error that the full production model
/// (via <c>UserProfile.CreatedBy</c>/<c>LastModifiedBy</c> self-referencing navigations) would trigger.
/// </summary>
public class CategoryTestDbContext(DbContextOptions<CategoryTestDbContext> options)
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

        builder.Entity<Category>(b =>
        {
            b.HasOne(c => c.CreatedBy).WithMany().HasForeignKey(c => c.CreatedById);
            b.HasOne(c => c.LastModifiedBy).WithMany().HasForeignKey(c => c.LastModifiedById);
            b.Ignore(c => c.Items);
        });

        builder.Ignore<ClassBalance>();
        builder.Ignore<Item>();
        builder.Ignore<Location>();
        builder.Ignore<SchoolClass>();
        builder.Ignore<StockBatch>();
        builder.Ignore<StockTransaction>();
        builder.Ignore<GoodsReceipt>();
    }
}

public class GetAllCategoriesHandlerTests
{
    private static async Task<CategoryTestDbContext> SeedAsync(int count, Func<int, Category>? factory = null)
    {
        var options = new DbContextOptionsBuilder<CategoryTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new CategoryTestDbContext(options);
        var baseline = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

        for (var i = 0; i < count; i++)
        {
            var category = factory?.Invoke(i) ?? new Category
            {
                Name = $"Category{i:D2}",
                CreatedDate = baseline.AddMinutes(i),
                LastModifiedDate = baseline.AddMinutes(i)
            };
            context.Categories.Add(category);
        }

        await context.SaveChangesAsync(CancellationToken.None);
        return context;
    }

    private static GetAllCategoriesHandler CreateHandler(IApplicationDbContext context) => new(context);

    // ---- Basic pagination -------------------------------------------------

    [Test]
    public async Task Handle_WithNoCursor_ReturnsFirstPageOrderedByDefaultSort()
    {
        await using var context = await SeedAsync(5);
        var handler = CreateHandler(context);

        var result = await handler.Handle(new GetAllCategoriesQuery { PageSize = 10 }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        var page = result.Value;
        page.Data.Count().ShouldBe(5);
        page.HasNextPage.ShouldBeFalse();
        // NextCursor is still populated from the last row even without a next page;
        // callers should rely on HasNextPage rather than NextCursor's nullness.
        page.NextCursor.ShouldNotBeNullOrWhiteSpace();

        // Default sort is CreatedDate desc, Id desc -> most-recently-created category first.
        page.Data.Select(c => c.Name).ShouldBe(["Category04", "Category03", "Category02", "Category01", "Category00"]);
    }

    [Test]
    public async Task Handle_WithPageSizeSmallerThanTotal_SetsHasNextPageAndReturnsCursor()
    {
        await using var context = await SeedAsync(5);
        var handler = CreateHandler(context);

        var result = await handler.Handle(new GetAllCategoriesQuery { PageSize = 2 }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        var page = result.Value;
        page.Data.Count().ShouldBe(2);
        page.HasNextPage.ShouldBeTrue();
        page.NextCursor.ShouldNotBeNullOrWhiteSpace();
        page.Data.Select(c => c.Name).ShouldBe(["Category04", "Category03"]);
    }

    [Test]
    public async Task Handle_PagingThroughCursors_ReturnsAllItemsExactlyOnceInOrder()
    {
        await using var context = await SeedAsync(11);
        var handler = CreateHandler(context);

        var collected = new List<CategoryDto>();
        string? cursor = null;
        var safetyCounter = 0;

        while (true)
        {
            safetyCounter++.ShouldBeLessThan(20); // guard against infinite loop on regression

            var result = await handler.Handle(
                new GetAllCategoriesQuery { PageSize = 3, Cursor = cursor },
                CancellationToken.None);

            result.IsSuccess.ShouldBeTrue();
            var page = result.Value;
            collected.AddRange(page.Data);

            if (!page.HasNextPage)
            {
                break;
            }

            page.NextCursor.ShouldNotBeNullOrWhiteSpace();
            cursor = page.NextCursor;
        }

        collected.Select(c => c.Id).Distinct().Count().ShouldBe(11); // no duplicates, no skips
        collected.Select(c => c.Name).ShouldBe(
            Enumerable.Range(0, 11).Reverse().Select(i => $"Category{i:D2}"));
    }

    [Test]
    public async Task Handle_WithEmptyDataSet_ReturnsEmptyPageWithNoCursor()
    {
        await using var context = await SeedAsync(0);
        var handler = CreateHandler(context);

        var result = await handler.Handle(new GetAllCategoriesQuery(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Data.ShouldBeEmpty();
        result.Value.HasNextPage.ShouldBeFalse();
        result.Value.NextCursor.ShouldBeNull();
    }

    [Test]
    public async Task Handle_WithPageSizeAboveMaximum_ClampsToDefaultPageSize()
    {
        await using var context = await SeedAsync(60);
        var handler = CreateHandler(context);

        var result = await handler.Handle(new GetAllCategoriesQuery { PageSize = 1000 }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Data.Count().ShouldBe(PaginationConstants.DEFAULT_PAGE_SIZE);
        result.Value.HasNextPage.ShouldBeTrue();
    }

    [Test]
    public async Task Handle_WithPageSizeBelowMinimum_ClampsToOne()
    {
        await using var context = await SeedAsync(5);
        var handler = CreateHandler(context);

        var result = await handler.Handle(new GetAllCategoriesQuery { PageSize = 0 }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Data.Count().ShouldBe(1);
        result.Value.HasNextPage.ShouldBeTrue();
    }

    // ---- Sorting ------------------------------------------------------------

    [Test]
    public async Task Handle_WithNameSortAscending_ReturnsItemsInAlphabeticalOrder()
    {
        await using var context = await SeedAsync(3, i => new Category
        {
            Name = i switch { 0 => "Charlie", 1 => "Alpha", _ => "Bravo" },
            CreatedDate = new DateTimeOffset(2024, 1, 1 + i, 0, 0, 0, TimeSpan.Zero),
            LastModifiedDate = new DateTimeOffset(2024, 1, 1 + i, 0, 0, 0, TimeSpan.Zero)
        });
        var handler = CreateHandler(context);

        var query = new GetAllCategoriesQuery
        {
            Sort = [new PaginationSort { Key = "name", Value = "ascend" }]
        };
        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Data.Select(c => c.Name).ShouldBe(["Alpha", "Bravo", "Charlie"]);
        result.Value.Sort.ShouldContain(s => s.Key == "name" && s.Value == "ascend");
    }

    [Test]
    public async Task Handle_WithNameSortDescending_ReturnsItemsInReverseAlphabeticalOrder()
    {
        await using var context = await SeedAsync(3, i => new Category
        {
            Name = i switch { 0 => "Charlie", 1 => "Alpha", _ => "Bravo" },
            CreatedDate = new DateTimeOffset(2024, 1, 1 + i, 0, 0, 0, TimeSpan.Zero),
            LastModifiedDate = new DateTimeOffset(2024, 1, 1 + i, 0, 0, 0, TimeSpan.Zero)
        });
        var handler = CreateHandler(context);

        var query = new GetAllCategoriesQuery
        {
            Sort = [new PaginationSort { Key = "name", Value = "descend" }]
        };
        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Data.Select(c => c.Name).ShouldBe(["Charlie", "Bravo", "Alpha"]);
    }

    [Test]
    public async Task Handle_WithUnknownSortKey_FallsBackToDefaultSort()
    {
        await using var context = await SeedAsync(3);
        var handler = CreateHandler(context);

        var query = new GetAllCategoriesQuery
        {
            Sort = [new PaginationSort { Key = "totallyUnknownKey", Value = "ascend" }]
        };
        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        // Falls back to default sort (Created desc, Id desc) since the key isn't recognized.
        result.Value.Data.Select(c => c.Name).ShouldBe(["Category02", "Category01", "Category00"]);
    }

    [Test]
    public async Task Handle_PagingWithCustomSort_RemainsConsistentAcrossPages()
    {
        await using var context = await SeedAsync(6, i => new Category
        {
            Name = $"Item{(char)('F' - i)}", // Item F, E, D, C, B, A
            CreatedDate = new DateTimeOffset(2024, 1, 1 + i, 0, 0, 0, TimeSpan.Zero),
            LastModifiedDate = new DateTimeOffset(2024, 1, 1 + i, 0, 0, 0, TimeSpan.Zero)
        });
        var handler = CreateHandler(context);

        var firstPage = await handler.Handle(new GetAllCategoriesQuery
        {
            PageSize = 4,
            Sort = [new PaginationSort { Key = "name", Value = "ascend" }]
        }, CancellationToken.None);

        firstPage.Value.Data.Select(c => c.Name).ShouldBe(["ItemA", "ItemB", "ItemC", "ItemD"]);
        firstPage.Value.HasNextPage.ShouldBeTrue();

        var secondPage = await handler.Handle(new GetAllCategoriesQuery
        {
            PageSize = 4,
            Cursor = firstPage.Value.NextCursor,
            Sort = [new PaginationSort { Key = "name", Value = "ascend" }]
        }, CancellationToken.None);

        secondPage.Value.Data.Select(c => c.Name).ShouldBe(["ItemE", "ItemF"]);
        secondPage.Value.HasNextPage.ShouldBeFalse();
    }

    // ---- Filtering ------------------------------------------------------------

    [Test]
    public async Task Handle_WithSearchTerm_FiltersByNameContains()
    {
        await using var context = await SeedAsync(3, i => new Category
        {
            Name = i switch { 0 => "Widgets", 1 => "Gadgets", _ => "Gizmos" },
            CreatedDate = new DateTimeOffset(2024, 1, 1 + i, 0, 0, 0, TimeSpan.Zero),
            LastModifiedDate = new DateTimeOffset(2024, 1, 1 + i, 0, 0, 0, TimeSpan.Zero)
        });
        var handler = CreateHandler(context);

        var result = await handler.Handle(new GetAllCategoriesQuery { SearchTerm = "get" }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Data.Select(c => c.Name).ShouldBe(["Gadgets", "Widgets"]);
    }

    [Test]
    public async Task Handle_WithColumnFilterEquals_ReturnsOnlyMatchingRow()
    {
        await using var context = await SeedAsync(5);
        var handler = CreateHandler(context);
        var targetId = context.Categories.Single(c => c.Name == "Category02").Id;

        var query = new GetAllCategoriesQuery
        {
            Filters = [new ColumnFilter("id", FilterOperator.Equals, targetId)]
        };
        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Data.Count().ShouldBe(1);
        result.Value.Data.Single().Name.ShouldBe("Category02");
    }

    [Test]
    public async Task Handle_WithColumnFilterContains_FiltersByNameSubstring()
    {
        await using var context = await SeedAsync(3, i => new Category
        {
            Name = i switch { 0 => "AlphaWidget", 1 => "BetaWidget", _ => "GammaGadget" },
            CreatedDate = new DateTimeOffset(2024, 1, 1 + i, 0, 0, 0, TimeSpan.Zero),
            LastModifiedDate = new DateTimeOffset(2024, 1, 1 + i, 0, 0, 0, TimeSpan.Zero)
        });
        var handler = CreateHandler(context);

        var query = new GetAllCategoriesQuery
        {
            Filters = [new ColumnFilter("name", FilterOperator.Contains, "Widget")]
        };
        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Data.Select(c => c.Name).ShouldBe(["BetaWidget", "AlphaWidget"]);
    }

    [Test]
    public async Task Handle_WithColumnFilterNotMatchingAnyRow_ReturnsEmptyPage()
    {
        await using var context = await SeedAsync(3);
        var handler = CreateHandler(context);

        var query = new GetAllCategoriesQuery
        {
            Filters = [new ColumnFilter("name", FilterOperator.Equals, "DoesNotExist")]
        };
        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Data.ShouldBeEmpty();
        result.Value.HasNextPage.ShouldBeFalse();
        result.Value.NextCursor.ShouldBeNull();
    }

    [Test]
    public async Task Handle_WithDateRangeFilter_ReturnsOnlyRowsWithinRange()
    {
        await using var context = await SeedAsync(5); // CreatedDate = baseline + i minutes, i = 0..4

        var handler = CreateHandler(context);
        var lower = new DateTimeOffset(2024, 1, 1, 0, 1, 0, TimeSpan.Zero); // i = 1
        var upper = new DateTimeOffset(2024, 1, 1, 0, 3, 0, TimeSpan.Zero); // i = 3

        var query = new GetAllCategoriesQuery
        {
            Filters = [new ColumnFilter("createdDate", FilterOperator.Between, $"{lower:O},{upper:O}")]
        };
        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Data.Select(c => c.Name).ShouldBe(["Category03", "Category02", "Category01"]);
    }

    [Test]
    public async Task Handle_WithFilterAndSortAndPagination_AppliesAllTogether()
    {
        await using var context = await SeedAsync(6, i => new Category
        {
            Name = i % 2 == 0 ? $"Alpha{i:D2}" : $"Beta{i:D2}",
            CreatedDate = new DateTimeOffset(2024, 1, 1 + i, 0, 0, 0, TimeSpan.Zero),
            LastModifiedDate = new DateTimeOffset(2024, 1, 1 + i, 0, 0, 0, TimeSpan.Zero)
        });
        var handler = CreateHandler(context);

        var query = new GetAllCategoriesQuery
        {
            PageSize = 2,
            Filters = [new ColumnFilter("name", FilterOperator.Contains, "Alpha")],
            Sort = [new PaginationSort { Key = "name", Value = "ascend" }]
        };
        var firstPage = await handler.Handle(query, CancellationToken.None);

        firstPage.IsSuccess.ShouldBeTrue();
        firstPage.Value.Data.Select(c => c.Name).ShouldBe(["Alpha00", "Alpha02"]);
        firstPage.Value.HasNextPage.ShouldBeTrue();

        var secondPageQuery = new GetAllCategoriesQuery
        {
            PageSize = 2,
            Filters = [new ColumnFilter("name", FilterOperator.Contains, "Alpha")],
            Sort = [new PaginationSort { Key = "name", Value = "ascend" }],
            Cursor = firstPage.Value.NextCursor
        };
        var secondPage = await handler.Handle(secondPageQuery, CancellationToken.None);

        secondPage.Value.Data.Select(c => c.Name).ShouldBe(["Alpha04"]);
        secondPage.Value.HasNextPage.ShouldBeFalse();
    }

    // ---- Cursor edge cases ------------------------------------------------

    [Test]
    public async Task Handle_WithMalformedCursor_IsIgnoredAndReturnsFirstPage()
    {
        await using var context = await SeedAsync(3);
        var handler = CreateHandler(context);

        var result = await handler.Handle(
            new GetAllCategoriesQuery { Cursor = "not-a-valid-cursor-token" },
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Data.Count().ShouldBe(3);
    }

    [Test]
    public async Task Handle_WithEmptyCursor_ReturnsFirstPage()
    {
        await using var context = await SeedAsync(3);
        var handler = CreateHandler(context);

        var result = await handler.Handle(
            new GetAllCategoriesQuery { Cursor = "" },
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Data.Count().ShouldBe(3);
    }
}

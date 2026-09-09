using FluentResults;
using Microsoft.EntityFrameworkCore;
using skestock.Application.Common.Filtering;
using skestock.Application.Common.Interfaces;
using skestock.Application.Common.Models;
using skestock.Application.Features.Items.Models;
using skestock.Application.Features.Items.Queries.GetAllItems;
using skestock.Domain.Entities;
using skestock.Domain.Queues;
using NUnit.Framework;
using Shouldly;

namespace skestock.Application.UnitTests.Features.Items.Queries.GetAllItems;

/// <summary>
/// Minimal <see cref="IApplicationDbContext"/> implementation for handler tests. Only maps
/// <see cref="Item"/> and the entities it references (Category, UserProfile for CreatedBy/
/// LastModifiedBy); all other DbSets required by the interface are left unmapped/ignored since
/// <see cref="GetAllItemsHandler"/> never touches them. Mirrors GetAllLocationsHandlerTests'
/// LocationTestDbContext.
/// </summary>
public class ItemTestDbContext(DbContextOptions<ItemTestDbContext> options)
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
            b.OwnsOne(c => c.Icon);
        });

        builder.Entity<Item>(b =>
        {
            b.HasOne(i => i.CreatedBy).WithMany().HasForeignKey(i => i.CreatedById);
            b.HasOne(i => i.LastModifiedBy).WithMany().HasForeignKey(i => i.LastModifiedById);
            b.HasOne(i => i.Category).WithMany(c => c.Items).HasForeignKey(i => i.CategoryId);
            b.Ignore(i => i.Batches);
            b.Ignore(i => i.Transactions);
            b.Ignore(i => i.ClassBalances);
        });

        builder.Ignore<Location>();
        builder.Ignore<SchoolClass>();
        builder.Ignore<StockBatch>();
        builder.Ignore<StockTransaction>();
        builder.Ignore<ClassBalance>();
        builder.Ignore<GoodsReceipt>();
    }
}

public class GetAllItemsHandlerTests
{
    private static async Task<ItemTestDbContext> SeedAsync(int count, Func<int, Category, Item>? factory = null)
    {
        var options = new DbContextOptionsBuilder<ItemTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new ItemTestDbContext(options);
        var category = new Category { Name = "Stationery" };
        context.Categories.Add(category);
        await context.SaveChangesAsync(CancellationToken.None);

        var baseline = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

        for (var i = 0; i < count; i++)
        {
            var item = factory?.Invoke(i, category) ?? new Item
            {
                Name = $"Item{i:D2}",
                Unit = "unit",
                CategoryId = category.Id,
                CreatedDate = baseline.AddMinutes(i),
                LastModifiedDate = baseline.AddMinutes(i)
            };
            context.Items.Add(item);
        }

        await context.SaveChangesAsync(CancellationToken.None);
        return context;
    }

    private static GetAllItemsHandler CreateHandler(IApplicationDbContext context) => new(context);

    [Test]
    public async Task Handle_WithNoCursor_ReturnsFirstPageOrderedByDefaultSort()
    {
        await using var context = await SeedAsync(5);
        var handler = CreateHandler(context);

        var result = await handler.Handle(new GetAllItemsQuery { PageSize = 10 }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        var page = result.Value;
        page.Data.Count().ShouldBe(5);
        page.HasNextPage.ShouldBeFalse();
        page.Data.Select(i => i.Name).ShouldBe(["Item04", "Item03", "Item02", "Item01", "Item00"]);
    }

    [Test]
    public async Task Handle_WithPageSizeSmallerThanTotal_SetsHasNextPageAndReturnsCursor()
    {
        await using var context = await SeedAsync(5);
        var handler = CreateHandler(context);

        var result = await handler.Handle(new GetAllItemsQuery { PageSize = 2 }, CancellationToken.None);

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

        var result = await handler.Handle(new GetAllItemsQuery(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Data.ShouldBeEmpty();
        result.Value.HasNextPage.ShouldBeFalse();
        result.Value.NextCursor.ShouldBeNull();
    }

    [Test]
    public async Task Handle_WithNameSortAscending_ReturnsItemsInAlphabeticalOrder()
    {
        await using var context = await SeedAsync(3, (i, category) => new Item
        {
            Name = i switch { 0 => "Charlie", 1 => "Alpha", _ => "Bravo" },
            Unit = "unit",
            CategoryId = category.Id,
            CreatedDate = new DateTimeOffset(2024, 1, 1 + i, 0, 0, 0, TimeSpan.Zero),
            LastModifiedDate = new DateTimeOffset(2024, 1, 1 + i, 0, 0, 0, TimeSpan.Zero)
        });
        var handler = CreateHandler(context);

        var query = new GetAllItemsQuery
        {
            Sort = [new PaginationSort { Key = "name", Value = "ascend" }]
        };
        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Data.Select(i => i.Name).ShouldBe(["Alpha", "Bravo", "Charlie"]);
        result.Value.Sort.ShouldContain(s => s.Key == "name" && s.Value == "ascend");
    }

    [Test]
    public async Task Handle_PagingThroughCursors_ReturnsAllItemsExactlyOnceInOrder()
    {
        await using var context = await SeedAsync(11);
        var handler = CreateHandler(context);

        var collected = new List<ItemDto>();
        string? cursor = null;
        var safetyCounter = 0;

        while (true)
        {
            safetyCounter++.ShouldBeLessThan(20);

            var result = await handler.Handle(
                new GetAllItemsQuery { PageSize = 3, Cursor = cursor },
                CancellationToken.None);

            result.IsSuccess.ShouldBeTrue();
            var page = result.Value;
            collected.AddRange(page.Data);

            if (!page.HasNextPage)
                break;

            cursor = page.NextCursor;
        }

        collected.Select(i => i.Id).Distinct().Count().ShouldBe(11);
        collected.Select(i => i.Name).ShouldBe(Enumerable.Range(0, 11).Reverse().Select(i => $"Item{i:D2}"));
    }

    [Test]
    public async Task Handle_WithSearchTerm_FiltersByNameOrSkuContains()
    {
        await using var context = await SeedAsync(3, (i, category) => new Item
        {
            Name = i switch { 0 => "Widgets", 1 => "Gadgets", _ => "Gizmos" },
            Sku = i == 2 ? "GAD-9" : null,
            Unit = "unit",
            CategoryId = category.Id,
            CreatedDate = new DateTimeOffset(2024, 1, 1 + i, 0, 0, 0, TimeSpan.Zero),
            LastModifiedDate = new DateTimeOffset(2024, 1, 1 + i, 0, 0, 0, TimeSpan.Zero)
        });
        var handler = CreateHandler(context);

        var result = await handler.Handle(new GetAllItemsQuery { SearchTerm = "GAD" }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Data.Select(i => i.Name).ShouldBe(["Gizmos"]);
    }

    [Test]
    public async Task Handle_WithColumnFilterEquals_ReturnsOnlyMatchingRow()
    {
        await using var context = await SeedAsync(5);
        var handler = CreateHandler(context);
        var targetId = context.Items.Single(i => i.Name == "Item02").Id;

        var query = new GetAllItemsQuery
        {
            Filters = [new ColumnFilter("id", FilterOperator.Equals, targetId)]
        };
        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Data.Count().ShouldBe(1);
        result.Value.Data.Single().Name.ShouldBe("Item02");
    }

    [Test]
    public async Task Handle_WithColumnFilterOnIsActive_ReturnsOnlyMatchingRows()
    {
        await using var context = await SeedAsync(3, (i, category) => new Item
        {
            Name = $"Item{i:D2}",
            Unit = "unit",
            CategoryId = category.Id,
            IsActive = i != 0,
            CreatedDate = new DateTimeOffset(2024, 1, 1 + i, 0, 0, 0, TimeSpan.Zero),
            LastModifiedDate = new DateTimeOffset(2024, 1, 1 + i, 0, 0, 0, TimeSpan.Zero)
        });
        var handler = CreateHandler(context);

        var query = new GetAllItemsQuery
        {
            Filters = [new ColumnFilter("isActive", FilterOperator.Equals, false)]
        };
        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Data.Count().ShouldBe(1);
        result.Value.Data.Single().Name.ShouldBe("Item00");
    }

    [Test]
    public async Task Handle_WithItem_ReturnsCategoryNameInDto()
    {
        await using var context = await SeedAsync(1);
        var handler = CreateHandler(context);

        var result = await handler.Handle(new GetAllItemsQuery(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        var dto = result.Value.Data.Single();
        dto.CategoryName.ShouldBe("Stationery");
    }

    [Test]
    public async Task Handle_WithMalformedCursor_IsIgnoredAndReturnsFirstPage()
    {
        await using var context = await SeedAsync(3);
        var handler = CreateHandler(context);

        var result = await handler.Handle(
            new GetAllItemsQuery { Cursor = "not-a-valid-cursor-token" },
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Data.Count().ShouldBe(3);
    }
}

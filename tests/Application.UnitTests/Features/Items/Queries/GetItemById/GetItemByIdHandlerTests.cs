using FluentResults;
using Microsoft.EntityFrameworkCore;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.Items.Queries.GetItemById;
using skestock.Domain.Entities;
using NUnit.Framework;
using Shouldly;

namespace skestock.Application.UnitTests.Features.Items.Queries.GetItemById;

/// <summary>
/// Minimal <see cref="IApplicationDbContext"/> implementation for handler tests. Only maps
/// <see cref="Item"/> and the entities it references (Category, UserProfile for CreatedBy/
/// LastModifiedBy); all other DbSets required by the interface are left unmapped/ignored since
/// <see cref="GetItemByIdHandler"/> never touches them. Mirrors GetLocationByIdHandlerTests'
/// LocationTestDbContext.
/// </summary>
public class ItemTestDbContext(DbContextOptions<ItemTestDbContext> options)
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

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<UserProfile>(b =>
        {
            b.Ignore(u => u.CreatedBy);
            b.Ignore(u => u.LastModifiedBy);
        });

        builder.Entity<Category>(b =>
        {
            b.HasOne(c => c.CreatedBy).WithMany().HasForeignKey(c => c.CreatedById);
            b.HasOne(c => c.LastModifiedBy).WithMany().HasForeignKey(c => c.LastModifiedById);
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

public class GetItemByIdHandlerTests
{
    private static ItemTestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ItemTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ItemTestDbContext(options);
    }

    [Test]
    public async Task Handle_WithExistingId_ReturnsMatchingItemDto()
    {
        await using var context = CreateContext();
        var category = new Category { Name = "Stationery" };
        context.Categories.Add(category);
        await context.SaveChangesAsync(CancellationToken.None);
        var item = new Item { Name = "Pencil", Sku = "PEN-1", Unit = "unit", CategoryId = category.Id };
        context.Items.Add(item);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetItemByIdHandler(context);
        var result = await handler.Handle(new GetItemByIdQuery { Id = item.Id }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Id.ShouldBe(item.Id);
        result.Value.Name.ShouldBe("Pencil");
        result.Value.Sku.ShouldBe("PEN-1");
    }

    [Test]
    public async Task Handle_WithItem_ReturnsCategoryNameInDto()
    {
        await using var context = CreateContext();
        var category = new Category { Name = "Stationery" };
        context.Categories.Add(category);
        await context.SaveChangesAsync(CancellationToken.None);
        var item = new Item { Name = "Pencil", Unit = "unit", CategoryId = category.Id };
        context.Items.Add(item);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetItemByIdHandler(context);
        var result = await handler.Handle(new GetItemByIdQuery { Id = item.Id }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.CategoryId.ShouldBe(category.Id);
        result.Value.CategoryName.ShouldBe("Stationery");
    }

    [Test]
    public async Task Handle_WithNonExistentId_ReturnsFailedResult()
    {
        await using var context = CreateContext();
        var handler = new GetItemByIdHandler(context);

        var result = await handler.Handle(new GetItemByIdQuery { Id = 12345 }, CancellationToken.None);

        result.IsFailed.ShouldBeTrue();
    }
}

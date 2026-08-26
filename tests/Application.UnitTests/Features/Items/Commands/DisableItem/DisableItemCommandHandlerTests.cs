using FluentResults;
using Microsoft.EntityFrameworkCore;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.Items.Commands.DisableItem;
using skestock.Domain.Entities;
using NUnit.Framework;
using Shouldly;

namespace skestock.Application.UnitTests.Features.Items.Commands.DisableItem;

/// <summary>
/// Minimal <see cref="IApplicationDbContext"/> implementation for handler/validator tests. See
/// CreateItemCommandHandlerTests' ItemTestDbContext for rationale - this mirrors it for the
/// DisableItem namespace.
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

public class DisableItemCommandHandlerTests
{
    private static async Task<(ItemTestDbContext Context, Item Item)> CreateContextWithItemAsync(bool isActive)
    {
        var options = new DbContextOptionsBuilder<ItemTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new ItemTestDbContext(options);
        var category = new Category { Name = "Stationery" };
        context.Categories.Add(category);
        await context.SaveChangesAsync(CancellationToken.None);

        var item = new Item { Name = "Pencil", Unit = "unit", CategoryId = category.Id, IsActive = isActive };
        context.Items.Add(item);
        await context.SaveChangesAsync(CancellationToken.None);

        return (context, item);
    }

    [Test]
    public async Task Handle_WithActiveItem_SetsIsActiveFalse()
    {
        var (context, item) = await CreateContextWithItemAsync(isActive: true);
        await using var _ = context;
        var handler = new DisableItemCommandHandler(context);

        var result = await handler.Handle(new DisableItemCommand { Id = item.Id }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.IsActive.ShouldBeFalse();

        var persisted = await context.Items.SingleAsync(i => i.Id == item.Id, CancellationToken.None);
        persisted.IsActive.ShouldBeFalse();
    }

    [Test]
    public async Task Handle_WithAlreadyInactiveItem_IsIdempotentAndSucceeds()
    {
        var (context, item) = await CreateContextWithItemAsync(isActive: false);
        await using var _ = context;
        var handler = new DisableItemCommandHandler(context);

        var result = await handler.Handle(new DisableItemCommand { Id = item.Id }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.IsActive.ShouldBeFalse();
    }

    [Test]
    public async Task Handle_WithNonExistentId_ReturnsFailedResult()
    {
        var (context, _) = await CreateContextWithItemAsync(isActive: true);
        await using var _disposable = context;
        var handler = new DisableItemCommandHandler(context);

        var result = await handler.Handle(new DisableItemCommand { Id = 12345 }, CancellationToken.None);

        result.IsFailed.ShouldBeTrue();
    }
}

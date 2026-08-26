using FluentResults;
using Microsoft.EntityFrameworkCore;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.SchoolClasses.Commands.UpdateSchoolClass;
using skestock.Domain.Entities;
using skestock.Domain.Enums;
using NUnit.Framework;
using Shouldly;

namespace skestock.Application.UnitTests.Features.SchoolClasses.Commands.UpdateSchoolClass;

/// <summary>
/// Minimal <see cref="IApplicationDbContext"/> implementation for handler/validator tests. See
/// CreateSchoolClassCommandHandlerTests' SchoolClassTestDbContext for rationale - this mirrors it
/// for the UpdateSchoolClass namespace.
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

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

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

public class UpdateSchoolClassCommandHandlerTests
{
    private static async Task<(SchoolClassTestDbContext Context, SchoolClass SchoolClass)> CreateContextWithClassAsync()
    {
        var options = new DbContextOptionsBuilder<SchoolClassTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new SchoolClassTestDbContext(options);
        var schoolClass = new SchoolClass
        {
            Name = "Old Name",
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 6, 1),
            Status = ClassStatus.Upcoming
        };
        context.SchoolClasses.Add(schoolClass);
        await context.SaveChangesAsync(CancellationToken.None);

        return (context, schoolClass);
    }

    [Test]
    public async Task Handle_WithExistingClass_UpdatesFieldsAndReturnsDto()
    {
        var (context, schoolClass) = await CreateContextWithClassAsync();
        await using var _ = context;
        var handler = new UpdateSchoolClassCommandHandler(context);

        var result = await handler.Handle(new UpdateSchoolClassCommand
        {
            Id = schoolClass.Id,
            Name = "New Name",
            StartDate = new DateOnly(2026, 2, 1),
            EndDate = new DateOnly(2026, 7, 1),
            Status = ClassStatus.Active
        }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Name.ShouldBe("New Name");
        result.Value.StartDate.ShouldBe(new DateOnly(2026, 2, 1));
        result.Value.EndDate.ShouldBe(new DateOnly(2026, 7, 1));
        result.Value.Status.ShouldBe(ClassStatus.Active);

        var persisted = await context.SchoolClasses.SingleAsync(c => c.Id == schoolClass.Id, CancellationToken.None);
        persisted.Name.ShouldBe("New Name");
        persisted.Status.ShouldBe(ClassStatus.Active);
    }

    [Test]
    public async Task Handle_WithNonExistentId_ReturnsFailedResult()
    {
        var (context, _) = await CreateContextWithClassAsync();
        await using var _disposable = context;
        var handler = new UpdateSchoolClassCommandHandler(context);

        var result = await handler.Handle(new UpdateSchoolClassCommand
        {
            Id = 12345,
            Name = "Anything",
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 6, 1)
        }, CancellationToken.None);

        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(e => e is skestock.Application.Common.Errors.SchoolClassErrors.SchoolClassNotFound);
    }
}

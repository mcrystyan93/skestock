using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using skestock.Domain.Entities.Categories;
using skestock.Domain.Entities.Items;
using skestock.Domain.Entities.Locations;
using skestock.Domain.Entities.OrderLists;
using skestock.Domain.Entities.SchoolClasses;
using skestock.Domain.Entities.Users;
using skestock.Infrastructure.Data;
using skestock.Infrastructure.Identity;

namespace skestock.Infrastructure.IntegrationTests;

public sealed class TemporalDeleteStampTests
{
    private Guid _actorId;
    private readonly List<Func<ApplicationDbContext, Task>> _cleanup = [];

    [SetUp]
    public async Task SetUp()
    {
        await using var dbContext = IntegrationTestSetup.CreateDbContext();
        var actor = new ApplicationUser
        {
            UserName = $"stamp-{Guid.NewGuid():N}",
            Email = $"stamp-{Guid.NewGuid():N}@test.local"
        };
        dbContext.Users.Add(actor);
        dbContext.UserProfiles.Add(new UserProfile
        {
            Id = Guid.CreateVersion7(), IdentityId = actor.Id, FirstName = "Stamp", LastName = "Actor"
        });
        await dbContext.SaveChangesAsync();
        _actorId = actor.Id;
        _cleanup.Add(async c =>
        {
            await c.UserProfiles.Where(u => u.IdentityId == actor.Id).ExecuteDeleteAsync();
            await c.Users.Where(u => u.Id == actor.Id).ExecuteDeleteAsync();
        });
    }

    [TearDown]
    public async Task TearDown()
    {
        await using var dbContext = IntegrationTestSetup.CreateDbContext();
        foreach (var cleanup in Enumerable.Reverse(_cleanup))
            await cleanup(dbContext);
        _cleanup.Clear();
    }

    [Test]
    public Task Deleting_a_rowversion_entity_records_the_deleter_without_a_concurrency_error() =>
        OrderListDeleteIsAttributed(inTransaction: false);

    [Test]
    public Task Deleting_a_rowversion_entity_inside_a_transaction_records_the_deleter() =>
        OrderListDeleteIsAttributed(inTransaction: true);

    [Test]
    public async Task A_failed_delete_inside_a_transaction_rolls_the_stamp_back()
    {
        var stampedId = await SeedOrderListAsync();
        var staleId = await SeedOrderListAsync();

        await using var actingContext = ActingContext();
        var stamped = await actingContext.OrderLists.SingleAsync(o => o.Id == stampedId);
        var stale = await actingContext.OrderLists.SingleAsync(o => o.Id == staleId);

        // Bump the rowversion behind the tracked copy so its DELETE (and stamp) match nothing and the save throws.
        await using (var other = IntegrationTestSetup.CreateDbContext())
        {
            await other.OrderLists.Where(o => o.Id == staleId).ExecuteUpdateAsync(s => s.SetProperty(o => o.Note, "bumped"));
        }

        actingContext.OrderLists.RemoveRange(stamped, stale);

        await actingContext.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var transaction = await actingContext.Database.BeginTransactionAsync();
            await Should.ThrowAsync<DbUpdateConcurrencyException>(() => actingContext.SaveChangesAsync());
            await transaction.RollbackAsync();
        });

        await using var dbContext = IntegrationTestSetup.CreateDbContext();
        (await dbContext.OrderLists.AnyAsync(o => o.Id == stampedId)).ShouldBeTrue();
        // The stamp UPDATE would have written a history version; it must have been rolled back with the DELETE.
        (await HistoryRowCountAsync("OrderListsHistory", "Id", stampedId)).ShouldBe(0);
    }

    [Test]
    public async Task Deleting_an_entity_without_auditable_base_records_the_deleter()
    {
        var thresholdId = await SeedThresholdAsync();

        await using var actingContext = ActingContext();
        var threshold = await actingContext.ItemLocationThresholds.SingleAsync(t => t.Id == thresholdId);
        actingContext.ItemLocationThresholds.Remove(threshold);
        await actingContext.SaveChangesAsync();

        (await NewestHistoryModifierAsync("ItemLocationThresholdsHistory", "Id", thresholdId)).ShouldBe(_actorId);
    }

    [Test]
    public async Task Removing_a_user_role_records_the_deleter()
    {
        var role = new IdentityRole<Guid>(Guid.NewGuid().ToString("N"));
        Guid userId;
        await using (var dbContext = IntegrationTestSetup.CreateDbContext())
        {
            var user = new ApplicationUser { UserName = $"role-{Guid.NewGuid():N}", Email = $"role-{Guid.NewGuid():N}@test.local" };
            userId = user.Id;
            dbContext.Users.Add(user);
            dbContext.UserProfiles.Add(new UserProfile
            {
                Id = Guid.CreateVersion7(), IdentityId = userId, FirstName = "Role", LastName = "Holder"
            });
            dbContext.Roles.Add(role);
            dbContext.UserRoles.Add(new IdentityUserRole<Guid> { UserId = userId, RoleId = role.Id });
            await dbContext.SaveChangesAsync();
        }

        _cleanup.Add(async c =>
        {
            await c.UserProfiles.Where(u => u.IdentityId == userId).ExecuteDeleteAsync();
            await c.Users.Where(u => u.Id == userId).ExecuteDeleteAsync();
            await c.Roles.Where(r => r.Id == role.Id).ExecuteDeleteAsync();
        });

        await using var actingContext = ActingContext();
        var userRole = await actingContext.UserRoles.SingleAsync(r => r.UserId == userId && r.RoleId == role.Id);
        actingContext.UserRoles.Remove(userRole);
        await actingContext.SaveChangesAsync();

        (await NewestHistoryModifierAsync("AspNetUserRolesHistory", "UserId", userId)).ShouldBe(_actorId);
    }

    private async Task OrderListDeleteIsAttributed(bool inTransaction)
    {
        var orderListId = await SeedOrderListAsync();

        await using var actingContext = ActingContext();
        var orderList = await actingContext.OrderLists.SingleAsync(o => o.Id == orderListId);
        actingContext.OrderLists.Remove(orderList);

        if (inTransaction)
        {
            await actingContext.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                await using var transaction = await actingContext.Database.BeginTransactionAsync();
                await actingContext.SaveChangesAsync();
                await transaction.CommitAsync();
            });
        }
        else
        {
            await actingContext.SaveChangesAsync();
        }

        (await actingContext.OrderLists.AnyAsync(o => o.Id == orderListId)).ShouldBeFalse();
        (await NewestHistoryModifierAsync("OrderListsHistory", "Id", orderListId)).ShouldBe(_actorId);
    }

    private ApplicationDbContext ActingContext() => ActingDbContext.Create(_actorId);

    private async Task<Guid> SeedOrderListAsync()
    {
        await using var dbContext = IntegrationTestSetup.CreateDbContext();
        var schoolClass = new SchoolClass
        {
            Id = Guid.CreateVersion7(),
            Name = $"Class-{Guid.NewGuid():N}",
            StartDate = new DateOnly(2031, 1, 1),
            EndDate = new DateOnly(2031, 12, 31)
        };
        var orderList = OrderList.Create(schoolClass.Id, "stamp", null);
        dbContext.AddRange(schoolClass, orderList);
        await dbContext.SaveChangesAsync();
        _cleanup.Add(async c =>
        {
            await c.OrderLists.Where(o => o.ClassId == schoolClass.Id).ExecuteDeleteAsync();
            await c.SchoolClasses.Where(s => s.Id == schoolClass.Id).ExecuteDeleteAsync();
        });
        return orderList.Id;
    }

    private async Task<Guid> SeedThresholdAsync()
    {
        await using var dbContext = IntegrationTestSetup.CreateDbContext();
        var category = new Category { Id = Guid.CreateVersion7(), Name = $"Cat-{Guid.NewGuid():N}" };
        var item = new Item { Id = Guid.CreateVersion7(), Name = $"Item-{Guid.NewGuid():N}", CategoryId = category.Id };
        var location = new Location { Id = Guid.CreateVersion7(), Name = "Kitchen", Type = "Kitchen" };
        var threshold = new ItemLocationThreshold { ItemId = item.Id, LocationId = location.Id, MinThreshold = 3 };
        dbContext.AddRange(category, item, location, threshold);
        await dbContext.SaveChangesAsync();
        _cleanup.Add(async c =>
        {
            await c.ItemLocationThresholds.Where(t => t.Id == threshold.Id).ExecuteDeleteAsync();
            await c.Items.Where(i => i.Id == item.Id).ExecuteDeleteAsync();
            await c.Categories.Where(x => x.Id == category.Id).ExecuteDeleteAsync();
            await c.Locations.Where(l => l.Id == location.Id).ExecuteDeleteAsync();
        });
        return threshold.Id;
    }

    private static async Task<int> HistoryRowCountAsync(string historyTable, string keyColumn, Guid key)
    {
        await using var connection = new SqlConnection(IntegrationTestSetup.DatabaseConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(*) FROM [dbo].[{historyTable}] WHERE [{keyColumn}] = @key";
        command.Parameters.AddWithValue("@key", key);
        return (int)(await command.ExecuteScalarAsync())!;
    }

    // Newest history version =the one the delete stamp wrote (zero-duration versions share PeriodStart/End).
    private static async Task<Guid?> NewestHistoryModifierAsync(string historyTable, string keyColumn, Guid key)
    {
        await using var connection = new SqlConnection(IntegrationTestSetup.DatabaseConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            SELECT TOP 1 [LastModifiedById] FROM [dbo].[{historyTable}]
            WHERE [{keyColumn}] = @key ORDER BY [PeriodEnd] DESC, [PeriodStart] DESC
            """;
        command.Parameters.AddWithValue("@key", key);
        var value = await command.ExecuteScalarAsync();
        value.ShouldNotBeNull();
        return value is DBNull ? null : (Guid)value;
    }
}

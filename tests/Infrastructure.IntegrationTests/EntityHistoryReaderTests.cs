using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Design.Internal;
using skestock.Application.Common.Errors;
using skestock.Application.Features.EntityHistory.Queries.GetEntityHistory;
using skestock.Domain.Entities.Locations;
using skestock.Domain.Entities.Users;
using skestock.Infrastructure.Data;
using skestock.Infrastructure.Identity;

namespace skestock.Infrastructure.IntegrationTests;

public sealed class EntityHistoryReaderTests
{
    private static async Task<Application.Common.Interfaces.EntityHistoryPage> ReadAsync(
        string entity, string id, int skip = 0, int take = 50)
    {
        await using var dbContext = IntegrationTestSetup.CreateDbContext();
        var result = await new EntityHistoryReader(dbContext).ReadAsync(entity, id, skip, take, CancellationToken.None);
        result.IsSuccess.ShouldBeTrue();
        return result.Value;
    }

    [TestCase("Items]; DROP TABLE Items--")]
    [TestCase("AspNetUserTokens")]
    [TestCase("AspNetUserRoles")]
    [TestCase("AspNetUserLogins")]
    [TestCase("OutboxMessages")]
    [TestCase("")]
    public async Task Unknown_or_denied_entities_are_rejected(string entity)
    {
        await using var dbContext = IntegrationTestSetup.CreateDbContext();

        var result = await new EntityHistoryReader(dbContext).ReadAsync(entity, Guid.NewGuid().ToString(), 0, 10, CancellationToken.None);

        result.IsFailed.ShouldBeTrue();
        result.Errors[0].ShouldBeOfType<EntityHistoryErrors.UnknownEntity>();
    }

    [Test]
    public async Task A_malformed_id_is_rejected()
    {
        await using var dbContext = IntegrationTestSetup.CreateDbContext();

        var result = await new EntityHistoryReader(dbContext).ReadAsync("Items", "not-a-guid", 0, 10, CancellationToken.None);

        result.Errors[0].ShouldBeOfType<EntityHistoryErrors.InvalidEntityId>();
    }

    [Test]
    public async Task Every_readable_table_executes_its_history_query()
    {
        await using var dbContext = IntegrationTestSetup.CreateDbContext();
        var failures = new List<string>();
        var checkedTables = new List<string>();
        foreach (var table in dbContext.GetService<IDesignTimeModel>().Model.GetRelationalModel().Tables
                     .Where(t => t.PrimaryKey is { Columns.Count: 1 }))
        {
            var id = table.PrimaryKey!.Columns[0].ProviderClrType == typeof(Guid) ? Guid.NewGuid().ToString()
                : table.PrimaryKey.Columns[0].ProviderClrType == typeof(int) ? "1" : "x";
            try
            {
                var result = await new EntityHistoryReader(dbContext).ReadAsync(table.Name, id, 0, 10, CancellationToken.None);
                if (result.IsSuccess)
                    checkedTables.Add(table.Name);
                else if (result.Errors[0] is not EntityHistoryErrors.UnknownEntity)
                    failures.Add($"{table.Name}: {result.Errors[0].Message}");
            }
            catch (Exception ex)
            {
                failures.Add($"{table.Name}: {ex.GetBaseException().Message}");
            }
        }
        failures.ShouldBeEmpty();
        checkedTables.ShouldContain("StockBatches");
        checkedTables.ShouldContain("StockTransactions");
    }

    [Test]
    public async Task Integer_keyed_tables_are_readable()
    {
        (await ReadAsync("SharedClassConfigurations", "1")).HasNextPage.ShouldBeFalse();
    }

    [Test]
    public async Task Identity_secrets_are_never_returned()
    {
        var user = new ApplicationUser { UserName = $"secret-{Guid.NewGuid():N}", Email = $"secret-{Guid.NewGuid():N}@test.local" };
        await using (var dbContext = IntegrationTestSetup.CreateDbContext())
        {
            user.PasswordHash = "hash";
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();
            user.PhoneNumber = "0700";
            await dbContext.SaveChangesAsync();
        }

        try
        {
            var page = await ReadAsync("AspNetUsers", user.Id.ToString());

            page.Versions.Count.ShouldBe(2);
            foreach (var version in page.Versions)
            {
                version.Values.ShouldContainKey("UserName");
                version.Values.Keys.ShouldNotContain("PasswordHash");
                version.Values.Keys.ShouldNotContain("SecurityStamp");
                version.Values.Keys.ShouldNotContain("ConcurrencyStamp");
            }
        }
        finally
        {
            await using var dbContext = IntegrationTestSetup.CreateDbContext();
            await dbContext.Users.Where(u => u.Id == user.Id).ExecuteDeleteAsync();
        }
    }

    [Test]
    public async Task Versions_are_paged_newest_first_with_names_as_they_were_and_a_deleted_hint()
    {
        var actor = new ApplicationUser { UserName = $"hist-{Guid.NewGuid():N}", Email = $"hist-{Guid.NewGuid():N}@test.local" };
        var profileId = Guid.CreateVersion7();
        await using (var dbContext = IntegrationTestSetup.CreateDbContext())
        {
            dbContext.Users.Add(actor);
            dbContext.UserProfiles.Add(new UserProfile { Id = profileId, IdentityId = actor.Id, FirstName = "Old", LastName = "Name" });
            await dbContext.SaveChangesAsync();
        }

        var location = new Location { Id = Guid.CreateVersion7(), Name = "v1", Type = "Kitchen" };
        try
        {
            await using (var acting = ActingDbContext.Create(actor.Id))
            {
                acting.Locations.Add(location);
                await acting.SaveChangesAsync();
                location.Name = "v2";
                await acting.SaveChangesAsync();
            }

            await using (var dbContext = IntegrationTestSetup.CreateDbContext())
            {
                await dbContext.UserProfiles.Where(p => p.Id == profileId).ExecuteUpdateAsync(s => s.SetProperty(p => p.FirstName, "New"));
            }

            await using (var acting = ActingDbContext.Create(actor.Id))
            {
                var tracked = await acting.Locations.SingleAsync(l => l.Id == location.Id);
                tracked.Name = "v3";
                await acting.SaveChangesAsync();
            }

            var all = await ReadAsync("Locations", location.Id.ToString());
            all.Versions.Select(v => v.Values["Name"]).ShouldBe(["v3", "v2", "v1"]);
            all.Versions[0].ValidTo.ShouldBeNull();
            all.Versions.Select(v => v.Operation).ShouldBe(
                [EntityHistoryOperation.Updated, EntityHistoryOperation.Updated, EntityHistoryOperation.Created]);
            all.Versions.Select(v => v.ModifiedById).ShouldAllBe(id => id == actor.Id);
            all.Versions.Select(v => v.ModifiedByName).ShouldBe(["New Name", "Old Name", "Old Name"]);

            var first = await ReadAsync("Locations", location.Id.ToString(), skip: 0, take: 2);
            first.HasNextPage.ShouldBeTrue();
            first.Versions.Count.ShouldBe(2);
            var second = await ReadAsync("Locations", location.Id.ToString(), skip: 2, take: 2);
            second.HasNextPage.ShouldBeFalse();
            second.Versions.Select(v => v.Values["Name"]).ShouldBe(["v1"]);

            await using (var acting = ActingDbContext.Create(actor.Id))
            {
                acting.Locations.Remove(await acting.Locations.SingleAsync(l => l.Id == location.Id));
                await acting.SaveChangesAsync();
            }

            var afterDelete = await ReadAsync("Locations", location.Id.ToString());
            afterDelete.Versions[0].Operation.ShouldBe(EntityHistoryOperation.Deleted);
            afterDelete.Versions[0].ModifiedById.ShouldBe(actor.Id);
        }
        finally
        {
            await using var dbContext = IntegrationTestSetup.CreateDbContext();
            await dbContext.Locations.Where(l => l.Id == location.Id).ExecuteDeleteAsync();
            await dbContext.UserProfiles.Where(p => p.Id == profileId).ExecuteDeleteAsync();
            await dbContext.Users.Where(u => u.Id == actor.Id).ExecuteDeleteAsync();
        }
    }
}

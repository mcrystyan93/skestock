using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using skestock.Infrastructure.Data;
using skestock.Domain.Entities.Categories;
using skestock.Domain.Entities.Items;
using skestock.Domain.Entities.Locations;
using skestock.Domain.Entities.SchoolClasses;
using skestock.Domain.Entities.Stock;
using skestock.Domain.Entities.Users;
using skestock.Domain.Enums;

namespace skestock.Application.FunctionalTests.Features.SchoolClasses.Commands.CloseSchoolClass;

public class CloseSchoolClassEndpointTests : TestBase
{
    private Item _item = null!;
    private Location _location = null!;

    [SetUp]
    public async Task SeedCatalog()
    {
        var category = new Category { Name = $"FT{Guid.NewGuid():N}"[..10] };
        await TestApp.AddAsync(category);
        _item = new Item { Name = "Flour", Unit = "kg", MinThreshold = 1, CategoryId = category.Id };
        await TestApp.AddAsync(_item);
        _location = new Location { Name = "Storage", Type = "StorageRoom" };
        await TestApp.AddAsync(_location);
    }

    private static async Task<SchoolClass> AddClassAsync(string name, ClassStatus status = ClassStatus.Active)
    {
        var schoolClass = new SchoolClass
        {
            Name = $"{name}-{Guid.NewGuid():N}"[..20],
            StartDate = new DateOnly(2026, 9, 1),
            EndDate = new DateOnly(2026, 12, 20),
            Status = status
        };
        await TestApp.AddAsync(schoolClass);
        return schoolClass;
    }

    private async Task<StockBatch> AddBatchAsync(SchoolClass owner, int quantity)
    {
        var batch = new StockBatch
        {
            ItemId = _item.Id,
            LocationId = _location.Id,
            ReceivedClassId = owner.Id,
            Quantity = quantity,
            UnitPrice = 2m,
            ReceivedDate = new DateOnly(2026, 9, 2)
        };
        await TestApp.AddAsync(batch);
        return batch;
    }

    private static async Task<HttpClient> LoginAsync(bool administrator)
    {
        var userId = administrator ? await TestApp.RunAsAdministratorAsync() : await TestApp.RunAsDefaultUserAsync();
        // Audit columns and stock transactions are FKs to UserProfile.IdentityId.
        await TestApp.AddAsync(new UserProfile { IdentityId = userId!.Value, FirstName = "Staff", LastName = "Member" });

        var client = FunctionalTestSetup.Factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/Users/login?useCookies=true", new
        {
            email = administrator ? "administrator@local" : "test@local",
            password = administrator ? "Administrator1234!" : "Testing1234!"
        });
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return client;
    }

    private static Task<HttpResponseMessage> CloseAsync(HttpClient client, Guid id, Guid targetClassId) =>
        client.PostAsJsonAsync($"/api/SchoolClasses/{id}/close", new { targetClassId });

    // The error code sits inside the problem details' "error" extension, so assert on the raw body.
    private static Task<string> ErrorCodeAsync(HttpResponseMessage response) =>
        response.Content.ReadAsStringAsync();

    private static async Task<List<T>> QueryAsync<T>(System.Linq.Expressions.Expression<Func<T, bool>> predicate)
        where T : class
    {
        using var scope = FunctionalTestSetup.ScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await context.Set<T>().AsNoTracking().Where(predicate).ToListAsync();
    }

    private static async Task<int> StockOfAsync(SchoolClass owner)
    {
        var batches = await QueryAsync<StockBatch>(b => b.ReceivedClassId == owner.Id);
        return batches.Sum(b => b.Quantity);
    }

    [Test]
    public async Task Close_RejectsAnonymousAndNonAdministrators()
    {
        var source = await AddClassAsync("src");
        var target = await AddClassAsync("tgt");

        using var anonymous = FunctionalTestSetup.Factory.CreateClient();
        (await CloseAsync(anonymous, source.Id, target.Id)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        using var user = await LoginAsync(administrator: false);
        (await CloseAsync(user, source.Id, target.Id)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        (await TestApp.FindAsync<SchoolClass>(source.Id))!.Status.ShouldBe(ClassStatus.Active);
    }

    [Test]
    public async Task Close_MovesStockToTargetAndClosesTheSource()
    {
        var source = await AddClassAsync("src");
        var target = await AddClassAsync("tgt", ClassStatus.Upcoming);
        await AddBatchAsync(source, 7);
        await AddBatchAsync(source, 0);
        await AddBatchAsync(target, 3);
        using var client = await LoginAsync(administrator: true);

        var response = await CloseAsync(client, source.Id, target.Id);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await TestApp.FindAsync<SchoolClass>(source.Id))!.Status.ShouldBe(ClassStatus.Closed);
        (await StockOfAsync(source)).ShouldBe(0);
        (await StockOfAsync(target)).ShouldBe(10);

        var transactions = await QueryAsync<StockTransaction>(t => t.ItemId == _item.Id);
        transactions.Count.ShouldBe(2);
        transactions.ShouldAllBe(t => t.Type == StockTransactionType.Rollover);
        transactions.Sum(t => t.QuantityChange).ShouldBe(0);

        var sourceBalance = (await QueryAsync<ClassBalance>(b => b.ClassId == source.Id)).ShouldHaveSingleItem();
        sourceBalance.ClosingQty.ShouldBe(7);
        var targetBalance = (await QueryAsync<ClassBalance>(b => b.ClassId == target.Id)).ShouldHaveSingleItem();
        targetBalance.OpeningQty.ShouldBe(7);
        targetBalance.ClosingQty.ShouldBeNull();
    }

    [Test]
    public async Task Close_ReturnsValidationProblemWhenTargetIsTheSource()
    {
        var source = await AddClassAsync("src");
        using var client = await LoginAsync(administrator: true);

        var response = await CloseAsync(client, source.Id, source.Id);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await TestApp.FindAsync<SchoolClass>(source.Id))!.Status.ShouldBe(ClassStatus.Active);
    }

    [Test]
    public async Task Close_ReturnsNotFoundForAnUnknownTarget()
    {
        var source = await AddClassAsync("src");
        using var client = await LoginAsync(administrator: true);

        var response = await CloseAsync(client, source.Id, Guid.NewGuid());

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task Close_ReturnsConflictWhenTheTargetIsClosed()
    {
        var source = await AddClassAsync("src");
        var target = await AddClassAsync("tgt", ClassStatus.Closed);
        await AddBatchAsync(source, 4);
        using var client = await LoginAsync(administrator: true);

        var response = await CloseAsync(client, source.Id, target.Id);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await ErrorCodeAsync(response)).ShouldContain("school_classes.target_not_open");
        (await StockOfAsync(source)).ShouldBe(4);
    }

    [Test]
    public async Task Close_RepeatedCloseReturnsConflictWithoutDoubleCounting()
    {
        var source = await AddClassAsync("src");
        var target = await AddClassAsync("tgt");
        await AddBatchAsync(source, 6);
        using var client = await LoginAsync(administrator: true);
        (await CloseAsync(client, source.Id, target.Id)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var second = await CloseAsync(client, source.Id, target.Id);

        second.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await ErrorCodeAsync(second)).ShouldContain("school_classes.already_closed");
        (await StockOfAsync(target)).ShouldBe(6);
        (await QueryAsync<ClassBalance>(b => b.ClassId == target.Id)).ShouldHaveSingleItem()
            .OpeningQty.ShouldBe(6);
    }

    [Test]
    public async Task Close_ConcurrentClosesTransferTheStockExactlyOnce()
    {
        var source = await AddClassAsync("src");
        var targetA = await AddClassAsync("tga");
        var targetB = await AddClassAsync("tgb");
        await AddBatchAsync(source, 9);
        using var client = await LoginAsync(administrator: true);

        var responses = await Task.WhenAll(
            CloseAsync(client, source.Id, targetA.Id),
            CloseAsync(client, source.Id, targetB.Id));

        responses.Count(r => r.StatusCode == HttpStatusCode.NoContent).ShouldBe(1);
        responses.Count(r => r.StatusCode == HttpStatusCode.Conflict).ShouldBe(1);
        (await StockOfAsync(source)).ShouldBe(0);
        ((await StockOfAsync(targetA)) + (await StockOfAsync(targetB))).ShouldBe(9);
    }

    [Test]
    public async Task Update_RejectsClosingAndReopeningThroughPut()
    {
        var open = await AddClassAsync("opn");
        var closed = await AddClassAsync("cls", ClassStatus.Closed);
        using var client = await LoginAsync(administrator: true);

        object Body(SchoolClass c, ClassStatus status) =>
            new { name = c.Name, startDate = c.StartDate, endDate = c.EndDate, status };

        var closing = await client.PutAsJsonAsync($"/api/SchoolClasses/{open.Id}", Body(open, ClassStatus.Closed));
        var reopening = await client.PutAsJsonAsync($"/api/SchoolClasses/{closed.Id}", Body(closed, ClassStatus.Active));

        closing.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await ErrorCodeAsync(closing)).ShouldContain("school_classes.status_change_not_allowed");
        reopening.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await TestApp.FindAsync<SchoolClass>(open.Id))!.Status.ShouldBe(ClassStatus.Active);
        (await TestApp.FindAsync<SchoolClass>(closed.Id))!.Status.ShouldBe(ClassStatus.Closed);
    }
}

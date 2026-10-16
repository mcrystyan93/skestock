using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using skestock.Infrastructure.Data;
using skestock.Application.Common.Errors;
using skestock.Application.Features.SchoolClasses.Commands.CloseSchoolClass;
using skestock.Application.Features.Stock.Commands.AdjustStock;
using skestock.Domain.Entities.SchoolClasses;
using skestock.Domain.Entities.Users;
using skestock.Domain.Enums;

namespace skestock.Application.FunctionalTests.Features.SchoolClasses.Commands.CloseSchoolClass;

/// <summary>
/// Closing a class and changing its data must be serialized by the database lock, not just by the status check.
/// A second connection plays the "other side" and holds its lock while we watch whether the request waits.
/// </summary>
public class ClassLockTests : TestBase
{
    private static readonly TimeSpan BlockedFor = TimeSpan.FromSeconds(1.5);

    private static async Task<SchoolClass> AddClassAsync()
    {
        var schoolClass = new SchoolClass
        {
            Name = $"cls-{Guid.NewGuid():N}"[..20],
            StartDate = new DateOnly(2026, 9, 1),
            EndDate = new DateOnly(2026, 12, 20),
            Status = ClassStatus.Active
        };
        await TestApp.AddAsync(schoolClass);
        return schoolClass;
    }

    private static async Task RunAsAdministratorWithProfileAsync()
    {
        var userId = await TestApp.RunAsAdministratorAsync();
        await TestApp.AddAsync(new UserProfile { IdentityId = userId!.Value, FirstName = "Staff", LastName = "Member" });
    }

    [Test]
    public async Task Close_WaitsForAChangeThatIsStillInFlight()
    {
        await RunAsAdministratorWithProfileAsync();
        var source = await AddClassAsync();
        var target = await AddClassAsync();

        using var scope = FunctionalTestSetup.ScopeFactory.CreateScope();
        var other = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await using var inFlight = await other.Database.BeginTransactionAsync();
        // What a guarded command holds until it commits.
        await other.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT * FROM [SchoolClasses] WITH (HOLDLOCK) WHERE [Id] = {source.Id}");

        var close = TestApp.SendAsync(new CloseSchoolClassCommand { Id = source.Id, TargetClassId = target.Id });

        (await Task.WhenAny(close, Task.Delay(BlockedFor))).ShouldNotBe(close);

        await inFlight.RollbackAsync();
        (await close).IsSuccess.ShouldBeTrue();
    }

    [Test]
    public async Task Change_WaitsForAnInProgressCloseAndThenSeesTheClassClosed()
    {
        await RunAsAdministratorWithProfileAsync();
        var source = await AddClassAsync();

        using var scope = FunctionalTestSetup.ScopeFactory.CreateScope();
        var closing = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await using var transaction = await closing.Database.BeginTransactionAsync();
        await closing.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE [SchoolClasses] SET [Status] = {(int)ClassStatus.Closed} WHERE [Id] = {source.Id}");

        var change = TestApp.SendAsync(new AdjustStockCommand { ClassId = source.Id });

        (await Task.WhenAny(change, Task.Delay(BlockedFor))).ShouldNotBe(change);

        await transaction.CommitAsync();
        (await change).Errors.ShouldContain(e => e is SchoolClassErrors.SchoolClassClosed);
    }
}

using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using Shouldly;
using skestock.Application.Common.Interfaces;
using skestock.Domain.Entities.Items;
using skestock.Infrastructure.Data;
using skestock.Infrastructure.Data.Interceptors;

namespace skestock.Application.UnitTests.Infrastructure.Data.Interceptors;

public class AuditableEntityInterceptorTests
{
    private static ApplicationDbContext CreateContext(Guid? userId)
    {
        var user = new Mock<IUser>();
        user.SetupGet(u => u.Id).Returns(userId);

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(new AuditableEntityInterceptor(user.Object, TimeProvider.System))
            .Options;
        return new ApplicationDbContext(options);
    }

    private static Guid? Modifier(ApplicationDbContext context, object entity) =>
        (Guid?)context.Entry(entity).Property(ApplicationDbContext.LastModifiedById).CurrentValue;

    [Test]
    public async Task ShouldStampShadowModifierOnAddedAndModifiedEntitiesWithoutAuditableBase()
    {
        var userId = Guid.NewGuid();
        await using var context = CreateContext(userId);
        var threshold = new ItemLocationThreshold { ItemId = Guid.NewGuid(), LocationId = Guid.NewGuid(), MinThreshold = 1 };
        context.ItemLocationThresholds.Add(threshold);

        await context.SaveChangesAsync();
        Modifier(context, threshold).ShouldBe(userId);

        context.Entry(threshold).Property(ApplicationDbContext.LastModifiedById).CurrentValue = null;
        threshold.MinThreshold = 2;
        await context.SaveChangesAsync();
        Modifier(context, threshold).ShouldBe(userId);
    }

    [Test]
    public async Task ShouldStampNullModifierForSystemActor()
    {
        await using var context = CreateContext(null);
        var threshold = new ItemLocationThreshold { ItemId = Guid.NewGuid(), LocationId = Guid.NewGuid(), MinThreshold = 1 };
        context.ItemLocationThresholds.Add(threshold);

        await context.SaveChangesAsync();

        Modifier(context, threshold).ShouldBeNull();
    }
}

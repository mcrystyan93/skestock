using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;
using skestock.Domain.Queues;
using skestock.Infrastructure.Data.Interceptors;

namespace skestock.Application.UnitTests.Infrastructure.Data.Interceptors;

[NonParallelizable]
public class OutboxTraceContextInterceptorTests
{
    [TearDown]
    public void TearDown() => Activity.Current = null;

    [Test]
    public async Task ShouldStampAddedOutboxMessageWithCurrentTraceContext()
    {
        using var activity = new Activity("request").SetIdFormat(ActivityIdFormat.W3C).Start();
        activity.TraceStateString = "vendor=value";
        await using var context = CreateContext();
        var message = NewMessage();

        context.OutboxMessages.Add(message);
        await context.SaveChangesAsync();

        message.TraceParent.ShouldBe(activity.Id);
        message.TraceState.ShouldBe("vendor=value");
    }

    [Test]
    public async Task ShouldPreserveExplicitTraceContext()
    {
        using var activity = new Activity("request").SetIdFormat(ActivityIdFormat.W3C).Start();
        await using var context = CreateContext();
        var message = NewMessage();
        message.TraceParent = "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01";

        context.OutboxMessages.Add(message);
        await context.SaveChangesAsync();

        message.TraceParent.ShouldBe("00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01");
    }

    [Test]
    public async Task ShouldLeaveTraceContextEmptyWithoutActivity()
    {
        Activity.Current = null;
        await using var context = CreateContext();
        var message = NewMessage();

        context.OutboxMessages.Add(message);
        await context.SaveChangesAsync();

        message.TraceParent.ShouldBeNull();
        message.TraceState.ShouldBeNull();
    }

    private static OutboxMessage NewMessage() => new()
    {
        Id = Guid.NewGuid(),
        Type = "Some.Type",
        Payload = "{}"
    };

    private static OutboxTestDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<OutboxTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(new OutboxTraceContextInterceptor())
            .Options);

    private sealed class OutboxTestDbContext(DbContextOptions<OutboxTestDbContext> options) : DbContext(options)
    {
        public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    }
}

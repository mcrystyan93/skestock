using System.Diagnostics;
using Microsoft.AspNetCore.SignalR.StackExchangeRedis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OpenTelemetry.Trace;
using skestock.Application.Common.Interfaces;
using StackExchange.Redis;

namespace skestock.Infrastructure.IntegrationTests;

[NonParallelizable]
public sealed class RedisTelemetryTests
{
    private const string RedisInstrumentationSource = "OpenTelemetry.Instrumentation.StackExchangeRedis";

    [Test]
    public async Task LockAndSignalR_ShareTheSingleDiMultiplexer()
    {
        using var host = InfrastructureHost.Build();
        var multiplexer = host.Services.GetRequiredService<IConnectionMultiplexer>();

        host.Services.GetServices<IConnectionMultiplexer>().Count().ShouldBe(1);

        var signalROptions = host.Services.GetRequiredService<IOptions<RedisOptions>>().Value;
        signalROptions.ConnectionFactory.ShouldNotBeNull();
        (await signalROptions.ConnectionFactory!(TextWriter.Null)).ShouldBeSameAs(multiplexer);

        await using var lease = await host.Services.GetRequiredService<IDistributedLock>()
            .TryAcquireAsync($"redis-telemetry-{Guid.NewGuid():N}", TimeSpan.FromSeconds(30), CancellationToken.None);
        lease.ShouldNotBeNull();
    }

    [Test]
    public void RedisHealthCheck_IsRegistered()
    {
        using var host = InfrastructureHost.Build();

        var options = host.Services.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;

        options.Registrations.ShouldContain(r => r.Name.Contains("redis", StringComparison.OrdinalIgnoreCase));
    }

    [Test]
    public async Task LockCommands_AreTracedByRedisInstrumentation()
    {
        var redisActivities = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == RedisInstrumentationSource,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity => { lock (redisActivities) redisActivities.Add(activity); }
        };
        ActivitySource.AddActivityListener(listener);

        using var host = InfrastructureHost.Build();
        _ = host.Services.GetRequiredService<TracerProvider>();

        var resource = $"redis-trace-{Guid.NewGuid():N}";
        await using (await host.Services.GetRequiredService<IDistributedLock>()
                         .TryAcquireAsync(resource, TimeSpan.FromSeconds(30), CancellationToken.None))
        {
        }

        // The instrumentation drains profiled commands on a background interval.
        var deadline = DateTime.UtcNow.AddSeconds(20);
        while (DateTime.UtcNow < deadline)
        {
            lock (redisActivities)
            {
                if (redisActivities.Count > 0) break;
            }
            await Task.Delay(250);
        }

        lock (redisActivities)
        {
            redisActivities.ShouldNotBeEmpty();
        }
    }
}

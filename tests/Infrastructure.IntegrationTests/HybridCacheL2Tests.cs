using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace skestock.Infrastructure.IntegrationTests;

/// <summary>
/// Two independent hosts stand in for two processes (e.g. Web and Worker, or a restart):
/// they share only Redis.
/// </summary>
public sealed class HybridCacheL2Tests
{
    [Test]
    public async Task ValueCachedByOneProcess_IsServedToAnotherFromRedis()
    {
        using var hostA = InfrastructureHost.Build();
        using var hostB = InfrastructureHost.Build();
        var key = $"l2-share-{Guid.NewGuid():N}";

        await hostA.Services.GetRequiredService<HybridCache>()
            .GetOrCreateAsync(key, _ => ValueTask.FromResult("from-a"));
        await WaitForBackgroundDistributedWriteAsync();

        var factoryCalled = false;
        var value = await hostB.Services.GetRequiredService<HybridCache>()
            .GetOrCreateAsync(key, _ =>
            {
                factoryCalled = true;
                return ValueTask.FromResult("from-b");
            });

        value.ShouldBe("from-a");
        factoryCalled.ShouldBeFalse();
    }

    [Test]
    public async Task RemovingATagInOneProcess_InvalidatesTheEntryInAnother()
    {
        using var hostA = InfrastructureHost.Build();
        using var hostB = InfrastructureHost.Build();
        var cacheA = hostA.Services.GetRequiredService<HybridCache>();
        var cacheB = hostB.Services.GetRequiredService<HybridCache>();
        var key = $"l2-tag-{Guid.NewGuid():N}";
        string[] tags = [$"l2-tag-{Guid.NewGuid():N}"];

        await cacheB.GetOrCreateAsync(key, _ => ValueTask.FromResult("original"), tags: tags);
        await WaitForBackgroundDistributedWriteAsync();

        await cacheA.RemoveByTagAsync(tags[0]);

        var deadline = DateTime.UtcNow.AddSeconds(10);
        string value;
        do
        {
            await Task.Delay(200);
            value = await cacheB.GetOrCreateAsync(key, _ => ValueTask.FromResult("recomputed"), tags: tags);
        } while (value != "recomputed" && DateTime.UtcNow < deadline);

        value.ShouldBe("recomputed");
    }

    // Distributed writes run in the background (AllowBackgroundDistributedCacheOperations).
    private static Task WaitForBackgroundDistributedWriteAsync() => Task.Delay(500);
}

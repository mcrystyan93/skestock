using FluentResults;
using Mediator;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using skestock.Application.Common.Behaviours;
using skestock.Application.Common.Caching;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace skestock.Application.UnitTests.Common.Behaviours;

public record InvalidationTestCommand(IReadOnlyCollection<string> Tags) : IRequest<Result<string>>, ICacheInvalidation;

public class CacheInvalidationBehaviorTests
{
    private HybridCache _cache = null!;
    private Mock<ILogger<CacheInvalidationBehavior<InvalidationTestCommand, Result<string>>>> _logger = null!;

    [SetUp]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddHybridCache();
        _cache = services.BuildServiceProvider().GetRequiredService<HybridCache>();
        _logger = new Mock<ILogger<CacheInvalidationBehavior<InvalidationTestCommand, Result<string>>>>();
    }

    /// <summary>Seeds a cache entry under <paramref name="key"/> tagged with <paramref name="tag"/>.</summary>
    private async Task Seed(string key, string tag) =>
        await _cache.GetOrCreateAsync(key, static _ => new ValueTask<string>("seeded"), tags: [tag]);

    /// <summary>Returns true if fetching <paramref name="key"/> now requires re-running the factory
    /// (i.e. the previously seeded entry is no longer cached).</summary>
    private async Task<bool> WasEvicted(string key, string tag)
    {
        var factoryRan = false;
        ValueTask<string> Factory(CancellationToken _)
        {
            factoryRan = true;
            return new ValueTask<string>("re-seeded");
        }

        await _cache.GetOrCreateAsync(key, Factory, tags: [tag]);
        return factoryRan;
    }

    private CacheInvalidationBehavior<InvalidationTestCommand, Result<string>> CreateBehavior() =>
        new(_cache, _logger.Object);

    [Test]
    public async Task Handle_OnSuccessfulCommand_InvalidatesTaggedCacheEntries()
    {
        await Seed("k1", "inv-tag");
        var behavior = CreateBehavior();
        var command = new InvalidationTestCommand(["inv-tag"]);

        var result = await behavior.Handle(command, static (_, _) => new ValueTask<Result<string>>(Result.Ok("done")), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        (await WasEvicted("k1", "inv-tag")).ShouldBeTrue();
    }

    [Test]
    public async Task Handle_OnFailedCommand_DoesNotInvalidateCache()
    {
        await Seed("k2", "inv-tag");
        var behavior = CreateBehavior();
        var command = new InvalidationTestCommand(["inv-tag"]);

        var result = await behavior.Handle(command, static (_, _) => new ValueTask<Result<string>>(Result.Fail<string>("boom")), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        (await WasEvicted("k2", "inv-tag")).ShouldBeFalse(); // still cached - not invalidated
    }

    [Test]
    public async Task Handle_WithNoTags_DoesNotAffectUnrelatedCacheEntries()
    {
        await Seed("k3", "unrelated-tag");
        var behavior = CreateBehavior();
        var command = new InvalidationTestCommand([]);

        await behavior.Handle(command, static (_, _) => new ValueTask<Result<string>>(Result.Ok("done")), CancellationToken.None);

        (await WasEvicted("k3", "unrelated-tag")).ShouldBeFalse();
    }

    [Test]
    public async Task Handle_ReturnsTheInnerResultUnchanged()
    {
        var behavior = CreateBehavior();
        var command = new InvalidationTestCommand(["inv-tag"]);

        var result = await behavior.Handle(command, static (_, _) => new ValueTask<Result<string>>(Result.Ok("payload")), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("payload");
    }
}

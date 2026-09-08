using FluentResults;
using Mediator;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using skestock.Application.Common.Behaviours;
using skestock.Application.Common.Caching;
using NUnit.Framework;
using Shouldly;

namespace skestock.Application.UnitTests.Common.Behaviours;

public record CacheableTestQuery(string CacheKeySuffix, bool Bypass = false) : IRequest<Result<string>>, ICacheableQuery
{
    public IReadOnlyCollection<string> Tags => ["test-tag"];
    public bool BypassCache => Bypass;
    public TimeSpan? SlidingExpiration => TimeSpan.FromMinutes(5);
    public string BuildCacheKey() => $"test:{CacheKeySuffix}";
}

public class CachingBehaviorTests
{
    private HybridCache _cache = null!;
    private int _handlerCalls;

    [SetUp]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddHybridCache();
        _cache = services.BuildServiceProvider().GetRequiredService<HybridCache>();
        _handlerCalls = 0;
    }

    private ValueTask<Result<string>> Next(CacheableTestQuery request, CancellationToken cancellationToken)
    {
        _handlerCalls++;
        return new ValueTask<Result<string>>(Result.Ok($"value-{_handlerCalls}"));
    }

    [Test]
    public async Task Handle_WithBypassCache_AlwaysCallsNextAndNeverReturnsCachedValue()
    {
        var behavior = new CachingBehavior<CacheableTestQuery, Result<string>>(_cache);
        var request = new CacheableTestQuery("key1", Bypass: true);

        var first = await behavior.Handle(request, Next, CancellationToken.None);
        var second = await behavior.Handle(request, Next, CancellationToken.None);

        first.Value.ShouldBe("value-1");
        second.Value.ShouldBe("value-2");
        _handlerCalls.ShouldBe(2);
    }

    [Test]
    public async Task Handle_OnCacheMissThenHit_OnlyCallsNextOnce()
    {
        var behavior = new CachingBehavior<CacheableTestQuery, Result<string>>(_cache);
        var request = new CacheableTestQuery("key2");

        var first = await behavior.Handle(request, Next, CancellationToken.None);
        var second = await behavior.Handle(request, Next, CancellationToken.None);

        first.Value.ShouldBe("value-1");
        second.Value.ShouldBe("value-1"); // returned from cache, handler not invoked again
        _handlerCalls.ShouldBe(1);
    }

    [Test]
    public async Task Handle_WithDifferentCacheKeys_CallsNextForEachDistinctKey()
    {
        var behavior = new CachingBehavior<CacheableTestQuery, Result<string>>(_cache);

        await behavior.Handle(new CacheableTestQuery("keyA"), Next, CancellationToken.None);
        await behavior.Handle(new CacheableTestQuery("keyB"), Next, CancellationToken.None);

        _handlerCalls.ShouldBe(2);
    }

    [Test]
    public async Task Handle_CachesFailedResultsToo()
    {
        ValueTask<Result<string>> FailingNext(CacheableTestQuery request, CancellationToken cancellationToken)
        {
            _handlerCalls++;
            return new ValueTask<Result<string>>(Result.Fail<string>("boom"));
        }

        var behavior = new CachingBehavior<CacheableTestQuery, Result<string>>(_cache);
        var request = new CacheableTestQuery("failkey");

        var first = await behavior.Handle(request, FailingNext, CancellationToken.None);
        var second = await behavior.Handle(request, FailingNext, CancellationToken.None);

        first.IsSuccess.ShouldBeFalse();
        second.IsSuccess.ShouldBeFalse();
        second.Errors[0].Message.ShouldBe("boom");
        _handlerCalls.ShouldBe(1); // the failure itself was cached; handler wasn't invoked again
    }

    [Test]
    public async Task Handle_AfterTagInvalidation_CallsNextAgain()
    {
        var behavior = new CachingBehavior<CacheableTestQuery, Result<string>>(_cache);
        var request = new CacheableTestQuery("key3");

        await behavior.Handle(request, Next, CancellationToken.None);
        await _cache.RemoveByTagAsync("test-tag");
        await behavior.Handle(request, Next, CancellationToken.None);

        _handlerCalls.ShouldBe(2);
    }
}

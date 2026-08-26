using FluentResults;
using skestock.Application.Common.Caching;
using NUnit.Framework;
using Shouldly;

namespace skestock.Application.UnitTests.Common.Caching;

public class ResultCacheTransformerTests
{
    [Test]
    public void ToResultCache_WithSuccessfulResult_MapsValueAndSuccessFlag()
    {
        var result = Result.Ok(42);

        var cache = result.ToResultCache();

        cache.IsSuccess.ShouldBeTrue();
        cache.Value.ShouldBe(42);
        cache.Errors.ShouldBeEmpty();
    }

    [Test]
    public void ToResultCache_WithFailedResult_MapsErrorMessagesAndTypes()
    {
        var result = Result.Fail<int>("boom");

        var cache = result.ToResultCache();

        cache.IsSuccess.ShouldBeFalse();
        cache.Value.ShouldBe(default);
        cache.Errors.Count.ShouldBe(1);
        cache.Errors[0].Message.ShouldBe("boom");
        cache.Errors[0].ErrorType.ShouldNotBeNullOrWhiteSpace();
    }

    [Test]
    public void ToResultCache_WithMultipleErrors_MapsAllOfThem()
    {
        var result = Result.Fail<int>(new List<string> { "error one", "error two" });

        var cache = result.ToResultCache();

        cache.Errors.Select(e => e.Message).ShouldBe(["error one", "error two"]);
    }

    [Test]
    public void ToResult_WithSuccessfulCache_ReturnsSuccessfulResult()
    {
        var cache = new ResultCache<int>(isSuccess: true, value: 7);

        var result = cache.ToResult();

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(7);
    }

    [Test]
    public void ToResult_WithFailedCache_ReturnsFailedResultWithMessages()
    {
        var cache = new ResultCache<int>(isSuccess: false, errors: [new CachedError("boom", "SomeErrorType")]);

        var result = cache.ToResult();

        result.IsSuccess.ShouldBeFalse();
        result.Errors.Select(e => e.Message).ShouldBe(["boom"]);
    }

    [Test]
    public void RoundTrip_PreservesSuccessValue()
    {
        var original = Result.Ok("hello");

        var roundTripped = original.ToResultCache().ToResult();

        roundTripped.IsSuccess.ShouldBeTrue();
        roundTripped.Value.ShouldBe("hello");
    }

    [Test]
    public void RoundTrip_PreservesFailureMessage()
    {
        var original = Result.Fail<string>("something went wrong");

        var roundTripped = original.ToResultCache().ToResult();

        roundTripped.IsSuccess.ShouldBeFalse();
        roundTripped.Errors.Select(e => e.Message).ShouldBe(["something went wrong"]);
    }
}

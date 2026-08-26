using skestock.Application.Common.Caching;
using NUnit.Framework;
using Shouldly;

namespace skestock.Application.UnitTests.Common.Caching;

public class SlidingExpirationHelperTests
{
    [Test]
    public void GetRandomizedSlidingExpiration_StaysWithinVarianceBounds()
    {
        var baseExpiration = TimeSpan.FromMinutes(5);
        var varianceInSeconds = 30;

        for (var i = 0; i < 200; i++)
        {
            var result = SlidingExpirationHelper.GetRandomizedSlidingExpiration(baseExpiration, varianceInSeconds);

            result.ShouldBeGreaterThanOrEqualTo(baseExpiration - TimeSpan.FromSeconds(varianceInSeconds));
            result.ShouldBeLessThan(baseExpiration + TimeSpan.FromSeconds(varianceInSeconds));
        }
    }

    [Test]
    public void GetRandomizedSlidingExpiration_WithZeroVariance_ReturnsBaseExpirationExactly()
    {
        var result = SlidingExpirationHelper.GetRandomizedSlidingExpiration(TimeSpan.FromMinutes(5), 0);

        result.ShouldBe(TimeSpan.FromMinutes(5));
    }

    [Test]
    public void GetRandomizedSlidingExpiration_ProducesVariationAcrossCalls()
    {
        var results = Enumerable.Range(0, 50)
            .Select(_ => SlidingExpirationHelper.GetRandomizedSlidingExpiration(TimeSpan.FromMinutes(5), 30))
            .Distinct()
            .Count();

        // Extremely unlikely all 50 randomized values collapse to the same offset.
        results.ShouldBeGreaterThan(1);
    }
}

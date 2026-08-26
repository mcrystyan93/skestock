namespace skestock.Application.Common.Caching;

public static class SlidingExpirationHelper
{
    public static TimeSpan GetRandomizedSlidingExpiration(TimeSpan baseExpiration, int varianceInSeconds)
    {
        var randomOffset = Random.Shared.Next(-varianceInSeconds, varianceInSeconds);
        return baseExpiration + TimeSpan.FromSeconds(randomOffset);
    }
}

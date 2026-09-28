using Microsoft.Extensions.Logging;

namespace skestock.Application.Common.Behaviours;

/// <summary>
/// Source-generated request pipeline log messages. Request payloads are deliberately never
/// logged: they can contain uploaded file content and personal data.
/// </summary>
internal static partial class RequestLogMessages
{
    [LoggerMessage(
        EventId = 1000,
        Level = LogLevel.Debug,
        Message = "Handling {RequestName} for user {UserId}")]
    public static partial void HandlingRequest(this ILogger logger, string requestName, Guid? userId);

    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Warning,
        Message = "Long running request {RequestName} ({ElapsedMilliseconds} ms) for user {UserId}")]
    public static partial void LongRunningRequest(
        this ILogger logger,
        string requestName,
        long elapsedMilliseconds,
        Guid? userId);

    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Error,
        Message = "Unhandled exception for request {RequestName}")]
    public static partial void UnhandledRequestException(this ILogger logger, Exception exception, string requestName);

    [LoggerMessage(
        EventId = 1003,
        Level = LogLevel.Warning,
        Message = "Discarded unreadable cache entry {CacheKey} for {RequestName}; recomputing")]
    public static partial void UnreadableCacheEntry(this ILogger logger, string cacheKey, string requestName);
}

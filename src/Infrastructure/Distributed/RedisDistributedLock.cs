using StackExchange.Redis;
using Microsoft.Extensions.Logging;
using skestock.Application.Common.Interfaces;

namespace skestock.Infrastructure.Distributed;

public sealed class RedisDistributedLock(
    IConnectionMultiplexer connection,
    ILogger<RedisDistributedLock> logger) : IDistributedLock
{
    public async Task<IAsyncDisposable?> TryAcquireAsync(
        string resource,
        TimeSpan expiry,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(resource))
        {
            throw new ArgumentException("A lock resource is required.", nameof(resource));
        }

        if (expiry <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(expiry), "Lock expiry must be positive.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var token = Guid.NewGuid().ToString("N");
        var database = connection.GetDatabase();
        var acquired = await database.LockTakeAsync(resource, token, expiry);

        return acquired
            ? new RedisLockLease(database, resource, token, logger)
            : null;
    }

    private sealed class RedisLockLease(
        IDatabase database,
        RedisKey resource,
        RedisValue token,
        ILogger<RedisDistributedLock> logger) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            try
            {
                await database.LockReleaseAsync(resource, token);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to release Redis lock {Resource}.", resource);
            }
        }
    }
}

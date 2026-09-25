namespace skestock.Application.Common.Interfaces;

public interface IDistributedLock
{
    Task<IAsyncDisposable?> TryAcquireAsync(
        string resource,
        TimeSpan expiry,
        CancellationToken cancellationToken);
}

using System.Globalization;
using Microsoft.Extensions.Options;

namespace Worker.Services;

/// <summary>
/// Periodically writes the heartbeat file while every queue loop is fresh. When a loop hangs the
/// file ages, Podman's health check fails and systemd restarts the container.
/// </summary>
public sealed class HeartbeatFileService(
    WorkerHeartbeat heartbeat,
    IOptions<WorkerHeartbeatOptions> options,
    TimeProvider timeProvider,
    ILogger<HeartbeatFileService> logger)
    : BackgroundService
{
    private static readonly TimeSpan WriteInterval = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(WriteInterval, timeProvider);
        try
        {
            do
            {
                try
                {
                    await TryWriteAsync(stoppingToken);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    logger.LogError(ex, "Could not write the Worker heartbeat file {HeartbeatFile}", options.Value.HeartbeatFile);
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal shutdown.
        }
    }

    /// <returns><see langword="true"/> when every queue was fresh and the file was written.</returns>
    public async Task<bool> TryWriteAsync(CancellationToken cancellationToken)
    {
        var staleQueues = heartbeat.GetStaleQueues();
        if (staleQueues.Count > 0)
        {
            logger.LogWarning(
                "Skipping the Worker heartbeat: queue loops {StaleQueues} have not completed an iteration in time",
                string.Join(", ", staleQueues));
            return false;
        }

        await File.WriteAllTextAsync(
            options.Value.HeartbeatFile,
            timeProvider.GetUtcNow().ToString("O", CultureInfo.InvariantCulture),
            cancellationToken);
        return true;
    }
}

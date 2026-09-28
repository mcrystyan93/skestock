using System.Collections.Concurrent;

namespace Worker.Services;

/// <summary>
/// Records when each queue polling loop last completed an iteration, so the Worker's container
/// health check can detect a hung loop. <see cref="HeartbeatFileService"/> turns the result into
/// the heartbeat file that Podman's <c>HealthCmd</c> inspects.
/// </summary>
public sealed class WorkerHeartbeat(TimeProvider timeProvider)
{
    private readonly ConcurrentDictionary<string, QueueBeat> _queues = new(StringComparer.Ordinal);

    /// <summary>Starts tracking a queue; it counts as fresh until <paramref name="maxStaleness"/> elapses.</summary>
    /// <returns>This instance, so callers can register while initialising a field.</returns>
    public WorkerHeartbeat Register(string queueName, TimeSpan maxStaleness)
    {
        _queues[queueName] = new QueueBeat(maxStaleness, timeProvider.GetUtcNow());
        return this;
    }

    public void Beat(string queueName)
    {
        if (_queues.TryGetValue(queueName, out var beat))
        {
            beat.Touch(timeProvider.GetUtcNow());
        }
    }

    /// <returns>Names of the queues whose loop has not beaten within its allowed staleness.</returns>
    public IReadOnlyList<string> GetStaleQueues()
    {
        var now = timeProvider.GetUtcNow();
        return _queues
            .Where(queue => now - queue.Value.LastBeat > queue.Value.MaxStaleness)
            .Select(queue => queue.Key)
            .Order(StringComparer.Ordinal)
            .ToList();
    }

    private sealed class QueueBeat(TimeSpan maxStaleness, DateTimeOffset lastBeat)
    {
        private long _lastBeatTicks = lastBeat.UtcTicks;

        public TimeSpan MaxStaleness { get; } = maxStaleness;

        public DateTimeOffset LastBeat => new(Interlocked.Read(ref _lastBeatTicks), TimeSpan.Zero);

        public void Touch(DateTimeOffset now) => Interlocked.Exchange(ref _lastBeatTicks, now.UtcTicks);
    }
}

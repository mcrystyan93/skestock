namespace Worker.Services;

public sealed class WorkerHeartbeatOptions
{
    /// <summary>
    /// File touched while every queue loop is healthy. Must match the path in the Worker
    /// quadlet's <c>HealthCmd</c>.
    /// </summary>
    public string HeartbeatFile { get; set; } = "/tmp/skestock-worker.heartbeat";
}

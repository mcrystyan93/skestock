using Microsoft.Extensions.Logging;

namespace skestock.Application.UnitTests.Common.Behaviours;

public sealed record CapturedLogEntry(
    LogLevel Level,
    string Message,
    IReadOnlyList<KeyValuePair<string, object?>> State,
    Exception? Exception);

public sealed class CapturingLogger<T>(LogLevel minimumLevel = LogLevel.Trace) : ILogger<T>
{
    public List<CapturedLogEntry> Entries { get; } = [];

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel >= minimumLevel;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        var values = state as IReadOnlyList<KeyValuePair<string, object?>> ?? [];
        Entries.Add(new CapturedLogEntry(logLevel, formatter(state, exception), values, exception));
    }
}

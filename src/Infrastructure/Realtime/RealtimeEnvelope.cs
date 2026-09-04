namespace skestock.Infrastructure.Realtime;

public sealed record RealtimeEnvelope<T>(Guid MessageId, T Data);

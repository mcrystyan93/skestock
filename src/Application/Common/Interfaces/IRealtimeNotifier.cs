namespace skestock.Application.Common.Interfaces;

public interface IRealtimeNotifier
{
    Task NotifyUserAsync<T>(string userId, string method, T payload, CancellationToken ct = default);
    Task NotifyGroupAsync<T>(string group, string method, T payload, CancellationToken ct = default);
}

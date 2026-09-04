using Microsoft.AspNetCore.SignalR;
using skestock.Application.Common.Interfaces;

namespace skestock.Infrastructure.Realtime;

public class SignalRRealtimeNotifier(IHubContext<AppHub> hubContext) : IRealtimeNotifier
{
    public Task NotifyUserAsync<T>(string userId, string method, T payload, CancellationToken ct = default)
    {
        var envelope = new RealtimeEnvelope<T>(Guid.NewGuid(), payload);
        return hubContext.Clients.Group($"user:{userId}").SendAsync(method, envelope, ct);
    }

    public Task NotifyGroupAsync<T>(string group, string method, T payload, CancellationToken ct = default)
    {
        var envelope = new RealtimeEnvelope<T>(Guid.NewGuid(), payload);
        return hubContext.Clients.Group(group).SendAsync(method, envelope, ct);
    }
}

using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using skestock.Application.Common.Realtime;

namespace skestock.Infrastructure.Realtime;

[Authorize]
public class AppHub: Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier; // wire up your auth to populate this
        if (userId is not null)
            await Groups.AddToGroupAsync(Context.ConnectionId, RealtimeGroups.User(userId));
        await base.OnConnectedAsync();
    }
    
    public Task JoinGroup(string groupName) => Groups.AddToGroupAsync(Context.ConnectionId, groupName);
    
    public Task LeaveGroup(string groupName) => Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
}

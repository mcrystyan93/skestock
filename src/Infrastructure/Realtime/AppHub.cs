using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;

namespace skestock.Infrastructure.Realtime;

[Authorize]
public class AppHub: Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier; // wire up your auth to populate this
        if (userId is not null)
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");
        await base.OnConnectedAsync();
    }
    
    public Task JoinGroup(string groupName) => Groups.AddToGroupAsync(Context.ConnectionId, groupName);
    
    public Task LeaveGroup(string groupName) => Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
}

using Microsoft.AspNetCore.SignalR;

namespace Backend.Hubs;

public class RobotsHub : Hub
{
    public Task JoinMap(int mapId)
    {
        return Groups.AddToGroupAsync(Context.ConnectionId, $"map:{mapId}");
    }

    public Task LeaveMap(int mapId)
    {
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, $"map:{mapId}");
    }
}

using BackendV3.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace BackendV3.Realtime;

[Authorize(Policy = AuthorizationPolicies.Viewer)]
public sealed class RealtimeHub : Hub
{
    public Task SendCommand(object _)
    {
        return Task.CompletedTask;
    }
}


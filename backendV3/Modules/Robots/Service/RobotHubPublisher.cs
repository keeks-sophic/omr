using BackendV3.Realtime;
using Microsoft.AspNetCore.SignalR;

namespace BackendV3.Modules.Robots.Service;

public sealed class RobotHubPublisher
{
    private readonly IHubContext<RealtimeHub> _hub;

    public RobotHubPublisher(IHubContext<RealtimeHub> hub)
    {
        _hub = hub;
    }

    public Task RobotMetaUpdatedAsync(string robotId, CancellationToken ct = default) =>
        _hub.Clients.All.SendAsync(SignalRRoutes.Events.RobotMetaUpdated, new { robotId }, ct);

    public Task RobotIdentityUpdatedAsync(string robotId, CancellationToken ct = default) =>
        _hub.Clients.All.SendAsync(SignalRRoutes.Events.RobotIdentityUpdated, new { robotId }, ct);

    public Task RobotCapabilityUpdatedAsync(string robotId, CancellationToken ct = default) =>
        _hub.Clients.All.SendAsync(SignalRRoutes.Events.RobotCapabilityUpdated, new { robotId }, ct);

    public Task RobotStatusUpdatedAsync(string robotId, object payload, CancellationToken ct = default) =>
        _hub.Clients.All.SendAsync(SignalRRoutes.Events.RobotStatusUpdated, new { robotId, payload }, ct);

    public Task RobotTelemetryUpdatedAsync(string robotId, object payload, CancellationToken ct = default) =>
        _hub.Clients.All.SendAsync(SignalRRoutes.Events.RobotTelemetryUpdated, new { robotId, payload }, ct);

    public Task RobotSettingsReportedUpdatedAsync(string robotId, object payload, CancellationToken ct = default) =>
        _hub.Clients.All.SendAsync(SignalRRoutes.Events.RobotSettingsReportedUpdated, new { robotId, payload }, ct);

    public Task RobotCommandAckAsync(string robotId, object payload, CancellationToken ct = default) =>
        _hub.Clients.All.SendAsync(SignalRRoutes.Events.RobotCommandAck, new { robotId, payload }, ct);
}


using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using BackendV2.Api.Service.Realtime;
using BackendV2.Api.Topics;
using BackendV2.Api.Dto.Realtime;
using BackendV2.Api.Dto.Fleet;
using BackendV2.Api.Dto.Traffic;
using BackendV2.Api.Dto.Core;

namespace BackendV2.Api.Hub;

[Authorize]
public class RealtimeHub : Microsoft.AspNetCore.SignalR.Hub
{
    private readonly RealtimeSnapshotService _snapshots;
    private readonly BackendV2.Api.Infrastructure.Persistence.AppDbContext _db;
    private readonly BackendV2.Api.Service.Realtime.HubThrottle _throttle = new BackendV2.Api.Service.Realtime.HubThrottle();
    public RealtimeHub(RealtimeSnapshotService snapshots, BackendV2.Api.Infrastructure.Persistence.AppDbContext db)
    {
        _snapshots = snapshots;
        _db = db;
    }

    public async System.Threading.Tasks.Task JoinFleet()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, SignalR.RealtimeGroups.Robots);
        var fleet = await _snapshots.GetFleetSummaryAsync();
        var traffic = await _snapshots.GetTrafficOverviewAsync();
        await Clients.Caller.SendAsync(SignalRTopics.FleetSummarySnapshot, new RealtimeMessage<FleetSummaryDto> { Topic = SignalRTopics.FleetSummarySnapshot, Payload = fleet });
        await Clients.Caller.SendAsync(SignalRTopics.TrafficOverviewSnapshot, new RealtimeMessage<TrafficOverviewDto> { Topic = SignalRTopics.TrafficOverviewSnapshot, Payload = traffic });
        var tasksOverview = await _snapshots.GetTasksOverviewSnapshotAsync();
        if (_throttle.CanSend(Context.ConnectionId, System.TimeSpan.FromMilliseconds(100))) await Clients.Caller.SendAsync(SignalRTopics.TaskStatusChanged, new RealtimeMessage<object> { Topic = SignalRTopics.TaskStatusChanged, Payload = tasksOverview });
        var mapOverview = await _snapshots.GetActiveMapSnapshotAsync();
        if (_throttle.CanSend(Context.ConnectionId, System.TimeSpan.FromMilliseconds(100))) await Clients.Caller.SendAsync(SignalRTopics.MapVersionCreated, new RealtimeMessage<object> { Topic = SignalRTopics.MapVersionCreated, Payload = mapOverview });
        var sub = Context.User?.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
        System.Guid? actor = System.Guid.TryParse(sub, out var g) ? g : null;
        await _db.AuditEvents.AddAsync(new BackendV2.Api.Model.Ops.AuditEvent { AuditEventId = System.Guid.NewGuid(), Timestamp = System.DateTimeOffset.UtcNow, ActorUserId = actor, Action = "hub.join_fleet", TargetType = "signalr", TargetId = "fleet", Outcome = "OK", DetailsJson = "{}" });
        await _db.SaveChangesAsync();
    }

    public Task JoinRobot(string robotId)
    {
        var robotsCsv = Context.User?.FindFirst("allowedRobotIds")?.Value ?? "";
        var roleCsv = Context.User?.FindFirst("roles")?.Value ?? "";
        var allowed = robotsCsv.Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries);
        var role = (roleCsv.Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries))[System.Math.Max(0, 0)];
        var authz = new SignalR.RealtimeAuthorizer();
        if (!authz.IsAllowed(role, robotId, allowed))
        {
            var sub = Context.User?.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
            System.Guid? actor = System.Guid.TryParse(sub, out var g) ? g : null;
            _ = _db.AuditEvents.AddAsync(new BackendV2.Api.Model.Ops.AuditEvent { AuditEventId = System.Guid.NewGuid(), Timestamp = System.DateTimeOffset.UtcNow, ActorUserId = actor, Action = "hub.join_robot", TargetType = "robot", TargetId = robotId, Outcome = "DENIED", DetailsJson = "{}" });
            _ = _db.SaveChangesAsync();
            throw new HubException("Not authorized to join robot group");
        }
        var group = SignalR.RealtimeGroups.Robot(robotId);
        var sub2 = Context.User?.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
        System.Guid? actor2 = System.Guid.TryParse(sub2, out var g2) ? g2 : null;
        _ = _db.AuditEvents.AddAsync(new BackendV2.Api.Model.Ops.AuditEvent { AuditEventId = System.Guid.NewGuid(), Timestamp = System.DateTimeOffset.UtcNow, ActorUserId = actor2, Action = "hub.join_robot", TargetType = "robot", TargetId = robotId, Outcome = "OK", DetailsJson = "{}" });
        _ = _db.SaveChangesAsync();
        return Groups.AddToGroupAsync(Context.ConnectionId, group);
    }

    public Task LeaveRobot(string robotId)
    {
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, SignalR.RealtimeGroups.Robot(robotId));
    }

    public async System.Threading.Tasks.Task RequestPresenceSnapshot(string robotId)
    {
        var robotsCsv = Context.User?.FindFirst("allowedRobotIds")?.Value ?? "";
        var allowed = robotsCsv.Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries);
        var roleCsv = Context.User?.FindFirst("roles")?.Value ?? "";
        var role = (roleCsv.Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries))[System.Math.Max(0, 0)];
        var authz = new SignalR.RealtimeAuthorizer();
        if (!authz.IsAllowed(role, robotId, allowed)) throw new HubException("Not authorized for robot presence");
        var session = await _db.RobotSessions.AsNoTracking().FirstOrDefaultAsync(x => x.RobotId == robotId);
        if (session == null) return;
        await Clients.Caller.SendAsync(SignalRTopics.RobotSessionUpdated, new RealtimeMessage<Dto.Core.RobotSessionDto> { Topic = SignalRTopics.RobotSessionUpdated, Payload = new Dto.Core.RobotSessionDto { RobotId = session.RobotId, Connected = session.Connected, LastSeen = session.LastSeen, RuntimeMode = session.RuntimeMode, SoftwareVersion = session.SoftwareVersion } });
        await Clients.Caller.SendAsync(SignalRTopics.RobotPresenceHeartbeat, new { robotId = session.RobotId, uptimeMs = 0 });
    }
    public async System.Threading.Tasks.Task RequestRobotSnapshots(string robotId)
    {
        var robotsCsv = Context.User?.FindFirst("allowedRobotIds")?.Value ?? "";
        var allowed = robotsCsv.Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries);
        var authz = new SignalR.RealtimeAuthorizer();
        var roleCsv = Context.User?.FindFirst("roles")?.Value ?? "";
        var role = (roleCsv.Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries))[System.Math.Max(0, 0)];
        if (!authz.IsAllowed(role, robotId, allowed)) throw new HubException("Not authorized for robot snapshots");
        var state = await _snapshots.GetRobotStateSnapshotAsync(robotId);
        var telemetry = await _snapshots.GetRobotTelemetrySnapshotAsync(robotId);
        var session = await _snapshots.GetRobotSessionAsync(robotId);
        await Clients.Caller.SendAsync(SignalRTopics.RobotStateSnapshot, new RealtimeMessage<RobotStateDto> { Topic = SignalRTopics.RobotStateSnapshot, Payload = state });
        await Clients.Caller.SendAsync(SignalRTopics.RobotTelemetrySnapshot, new RealtimeMessage<object> { Topic = SignalRTopics.RobotTelemetrySnapshot, Payload = telemetry });
        if (session != null)
        {
            await Clients.Caller.SendAsync(SignalRTopics.RobotSessionUpdated, new RealtimeMessage<RobotSessionDto> { Topic = SignalRTopics.RobotSessionUpdated, Payload = session });
        }
    }
}

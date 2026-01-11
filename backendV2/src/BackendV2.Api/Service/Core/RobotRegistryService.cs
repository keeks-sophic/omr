using System;
using System.Text.Json;
using System.Threading.Tasks;
using BackendV2.Api.Contracts.Presence;
using BackendV2.Api.Infrastructure.Persistence;
using BackendV2.Api.Topics;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace BackendV2.Api.Service.Core;

public class RobotRegistryService
{
    private readonly AppDbContext _db;
    private readonly IHubContext<BackendV2.Api.Hub.RealtimeHub> _hub;
    private readonly BackendV2.Api.Service.Config.ConfigurationService _config;
    public RobotRegistryService(AppDbContext db, IHubContext<BackendV2.Api.Hub.RealtimeHub> hub, BackendV2.Api.Service.Config.ConfigurationService config)
    {
        _db = db;
        _hub = hub;
        _config = config;
    }

    public async Task HandleHelloAsync(string robotId, PresenceHello hello, string? name = null, string? ip = null, string? softwareVersion = null)
    {
        var r = await _db.Robots.FirstOrDefaultAsync(x => x.RobotId == robotId);
        if (r == null)
        {
            r = new Model.Core.Robot { RobotId = robotId, Name = name, Ip = ip, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow, Connected = true, LastActive = DateTimeOffset.UtcNow };
            await _db.Robots.AddAsync(r);
        }
        else
        {
            r.Name = name ?? r.Name;
            r.Ip = ip ?? r.Ip;
            r.Connected = true;
            r.LastActive = DateTimeOffset.UtcNow;
            r.UpdatedAt = DateTimeOffset.UtcNow;
        }

        var session = await _db.RobotSessions.FirstOrDefaultAsync(x => x.RobotId == robotId);
        if (session == null)
        {
            session = new Model.Core.RobotSession
            {
                RobotId = robotId,
                Connected = true,
                LastSeen = DateTimeOffset.UtcNow,
                RuntimeMode = hello.RuntimeMode,
                SoftwareVersion = softwareVersion,
                CapabilitiesJson = JsonSerializer.Serialize(hello.Capabilities),
                FeatureFlagsJson = JsonSerializer.Serialize(hello.FeatureFlags),
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await _db.RobotSessions.AddAsync(session);
        }
        else
        {
            session.Connected = true;
            session.LastSeen = DateTimeOffset.UtcNow;
            session.RuntimeMode = hello.RuntimeMode;
            session.SoftwareVersion = softwareVersion ?? session.SoftwareVersion;
            session.CapabilitiesJson = JsonSerializer.Serialize(hello.Capabilities);
            session.FeatureFlagsJson = JsonSerializer.Serialize(hello.FeatureFlags);
            session.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await _db.SaveChangesAsync();
        await _hub.Clients.Group(BackendV2.Api.SignalR.RealtimeGroups.Robots).SendAsync(SignalRTopics.RobotSessionUpdated, new { robotId, connected = true, lastSeen = session.LastSeen, runtimeMode = session.RuntimeMode, softwareVersion = session.SoftwareVersion });
        await _hub.Clients.Group(BackendV2.Api.SignalR.RealtimeGroups.Robot(robotId)).SendAsync(SignalRTopics.RobotSessionUpdated, new { robotId, connected = true, lastSeen = session.LastSeen, runtimeMode = session.RuntimeMode, softwareVersion = session.SoftwareVersion });
        await _hub.Clients.Group(BackendV2.Api.SignalR.RealtimeGroups.Robots).SendAsync(SignalRTopics.RobotPresenceHello, new { robotId, runtimeMode = hello.RuntimeMode });
        await _config.PublishCurrentAsync(robotId);
    }

    public async Task HandleHeartbeatAsync(string robotId)
    {
        var session = await _db.RobotSessions.FirstOrDefaultAsync(x => x.RobotId == robotId);
        if (session != null)
        {
            var previous = session.LastSeen;
            session.Connected = true;
            session.LastSeen = DateTimeOffset.UtcNow;
            session.UpdatedAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync();
            var reconnect = previous.AddMinutes(2) < session.LastSeen;
            if (reconnect)
            {
                await _hub.Clients.Group(BackendV2.Api.SignalR.RealtimeGroups.Robots).SendAsync(SignalRTopics.RobotSessionUpdated, new { robotId, connected = true, lastSeen = session.LastSeen });
                await _hub.Clients.Group(BackendV2.Api.SignalR.RealtimeGroups.Robot(robotId)).SendAsync(SignalRTopics.RobotSessionUpdated, new { robotId, connected = true, lastSeen = session.LastSeen });
            }
            await _hub.Clients.Group(BackendV2.Api.SignalR.RealtimeGroups.Robots).SendAsync(SignalRTopics.RobotPresenceHeartbeat, new { robotId });
        }
    }
}

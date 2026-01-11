using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using BackendV2.Api.Dto.Sim;
using BackendV2.Api.Infrastructure.Persistence;
using BackendV2.Api.Model.Core;
using BackendV2.Api.Model.Sim;
using BackendV2.Api.Topics;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace BackendV2.Api.Service.Sim;

public class SimulationService
{
    private readonly AppDbContext _db;
    private readonly IHubContext<BackendV2.Api.Hub.RealtimeHub> _hub;
    public SimulationService(AppDbContext db, IHubContext<BackendV2.Api.Hub.RealtimeHub> hub)
    {
        _db = db;
        _hub = hub;
    }

    public async Task<SimSession> CreateAsync(SimSessionCreateRequest req, Guid? actorUserId)
    {
        var session = new SimSession
        {
            SimSessionId = Guid.NewGuid(),
            MapVersionId = req.MapVersionId,
            Status = "CREATED",
            SpeedMultiplier = req.SpeedMultiplier,
            CreatedBy = actorUserId,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            ConfigJson = JsonSerializer.Serialize(new { robots = req.Robots })
        };
        await _db.SimSessions.AddAsync(session);
        await _db.SaveChangesAsync();
        await _hub.Clients.Group(BackendV2.Api.SignalR.RealtimeGroups.Robots).SendCoreAsync(SignalRTopics.SimSessionStatus, new object[] { new { simSessionId = session.SimSessionId.ToString(), status = session.Status } }, System.Threading.CancellationToken.None);
        return session;
    }

    public async Task StartAsync(Guid simSessionId)
    {
        var session = await _db.SimSessions.FirstOrDefaultAsync(x => x.SimSessionId == simSessionId) ?? throw new InvalidOperationException("Sim session not found");
        session.Status = "RUNNING";
        session.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();
        await RegisterRobotsAsync(session);
        await _hub.Clients.Group(BackendV2.Api.SignalR.RealtimeGroups.Robots).SendCoreAsync(SignalRTopics.SimSessionStatus, new object[] { new { simSessionId = simSessionId.ToString(), status = session.Status } }, System.Threading.CancellationToken.None);
    }

    public async Task StopAsync(Guid simSessionId)
    {
        var session = await _db.SimSessions.FirstOrDefaultAsync(x => x.SimSessionId == simSessionId) ?? throw new InvalidOperationException("Sim session not found");
        session.Status = "STOPPED";
        session.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();
        await _hub.Clients.Group(BackendV2.Api.SignalR.RealtimeGroups.Robots).SendCoreAsync(SignalRTopics.SimSessionStatus, new object[] { new { simSessionId = simSessionId.ToString(), status = session.Status } }, System.Threading.CancellationToken.None);
    }

    public async Task PauseAsync(Guid simSessionId)
    {
        var session = await _db.SimSessions.FirstOrDefaultAsync(x => x.SimSessionId == simSessionId) ?? throw new InvalidOperationException("Sim session not found");
        session.Status = "PAUSED";
        session.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();
        await _hub.Clients.Group(BackendV2.Api.SignalR.RealtimeGroups.Robots).SendCoreAsync(SignalRTopics.SimSessionStatus, new object[] { new { simSessionId = simSessionId.ToString(), status = session.Status } }, System.Threading.CancellationToken.None);
    }

    public async Task ResumeAsync(Guid simSessionId)
    {
        var session = await _db.SimSessions.FirstOrDefaultAsync(x => x.SimSessionId == simSessionId) ?? throw new InvalidOperationException("Sim session not found");
        session.Status = "RUNNING";
        session.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();
        await _hub.Clients.Group(BackendV2.Api.SignalR.RealtimeGroups.Robots).SendCoreAsync(SignalRTopics.SimSessionStatus, new object[] { new { simSessionId = simSessionId.ToString(), status = session.Status } }, System.Threading.CancellationToken.None);
    }

    private async Task RegisterRobotsAsync(SimSession session)
    {
        var config = JsonSerializer.Deserialize<Dictionary<string, int>>(session.ConfigJson) ?? new Dictionary<string, int>();
        config.TryGetValue("robots", out var robots);
        robots = Math.Max(robots, 1);
        for (int i = 0; i < robots; i++)
        {
            var robotId = $"SIM-{session.SimSessionId.ToString("N").Substring(0, 6)}-{i + 1}";
            var robot = await _db.Robots.FirstOrDefaultAsync(r => r.RobotId == robotId);
            if (robot == null)
            {
                robot = new Robot { RobotId = robotId, Name = robotId, MapVersionId = session.MapVersionId, Location = new Point(0, 0) { SRID = 0 }, Connected = true, State = "IDLE", Battery = 100, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow };
                await _db.Robots.AddAsync(robot);
            }
            var rs = await _db.RobotSessions.FirstOrDefaultAsync(s => s.RobotId == robotId);
            if (rs == null)
            {
                rs = new RobotSession { RobotId = robotId, Connected = true, LastSeen = DateTimeOffset.UtcNow, RuntimeMode = "SIM", CapabilitiesJson = JsonSerializer.Serialize(new { supportsRotate = true, supportsTelescope = true }), FeatureFlagsJson = JsonSerializer.Serialize(new { telescopeEnabled = true }), UpdatedAt = DateTimeOffset.UtcNow };
                await _db.RobotSessions.AddAsync(rs);
            }
        }
        await _db.SaveChangesAsync();
    }
}

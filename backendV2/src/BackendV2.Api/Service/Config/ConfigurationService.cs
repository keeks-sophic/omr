using System;
using System.Text.Json;
using System.Threading.Tasks;
using BackendV2.Api.Dto.Config;
using BackendV2.Api.Dto.Robots;
using BackendV2.Api.Infrastructure.Persistence;
using BackendV2.Api.Service.Tasks;
using BackendV2.Api.Topics;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace BackendV2.Api.Service.Config;

public class ConfigurationService
{
    private readonly AppDbContext _db;
    private readonly IHubContext<BackendV2.Api.Hub.RealtimeHub> _hub;
    private readonly NatsPublisherStub _nats;
    public ConfigurationService(AppDbContext db, IHubContext<BackendV2.Api.Hub.RealtimeHub> hub, NatsPublisherStub nats)
    {
        _db = db;
        _hub = hub;
        _nats = nats;
    }

    public async Task UpdateMotionLimitsAsync(string robotId, MotionLimitsDto limits, Guid? actorUserId)
    {
        ValidateLimits(limits);
        var session = await _db.RobotSessions.FirstOrDefaultAsync(x => x.RobotId == robotId) ?? throw new InvalidOperationException("Robot session not found");
        session.MotionLimitsJson = JsonSerializer.Serialize(limits);
        session.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();
        await _db.RobotEvents.AddAsync(new BackendV2.Api.Model.Replay.RobotEvent { EventId = Guid.NewGuid(), RobotId = robotId, Timestamp = DateTimeOffset.UtcNow, Type = "config.motion_limits", Payload = session.MotionLimitsJson });
        await _db.SaveChangesAsync();
        await _nats.PublishCfgMotionLimitsAsync(robotId, limits);
        await _hub.Clients.Group(BackendV2.Api.SignalR.RealtimeGroups.Robots).SendCoreAsync(SignalRTopics.RobotConfigUpdated, new object[] { new { robotId, motionLimits = limits } }, System.Threading.CancellationToken.None);
    }

    public async Task UpdateRuntimeModeAsync(string robotId, string runtimeMode, Guid? actorUserId)
    {
        if (string.IsNullOrWhiteSpace(runtimeMode)) throw new InvalidOperationException("Invalid runtime mode");
        var session = await _db.RobotSessions.FirstOrDefaultAsync(x => x.RobotId == robotId) ?? throw new InvalidOperationException("Robot session not found");
        session.RuntimeMode = runtimeMode;
        session.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();
        await _db.RobotEvents.AddAsync(new BackendV2.Api.Model.Replay.RobotEvent { EventId = Guid.NewGuid(), RobotId = robotId, Timestamp = DateTimeOffset.UtcNow, Type = "config.runtime_mode", Payload = JsonSerializer.Serialize(new { runtimeMode }) });
        await _db.SaveChangesAsync();
        await _nats.PublishCfgRuntimeModeAsync(robotId, runtimeMode);
        await _hub.Clients.Group(BackendV2.Api.SignalR.RealtimeGroups.Robots).SendCoreAsync(SignalRTopics.RobotConfigUpdated, new object[] { new { robotId, runtimeMode } }, System.Threading.CancellationToken.None);
    }

    public async Task UpdateFeatureFlagsAsync(string robotId, RobotFeatureFlagsDto flags, Guid? actorUserId)
    {
        var session = await _db.RobotSessions.FirstOrDefaultAsync(x => x.RobotId == robotId) ?? throw new InvalidOperationException("Robot session not found");
        session.FeatureFlagsJson = JsonSerializer.Serialize(flags);
        session.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();
        await _db.RobotEvents.AddAsync(new BackendV2.Api.Model.Replay.RobotEvent { EventId = Guid.NewGuid(), RobotId = robotId, Timestamp = DateTimeOffset.UtcNow, Type = "config.feature_flags", Payload = session.FeatureFlagsJson });
        await _db.SaveChangesAsync();
        await _nats.PublishCfgFeaturesAsync(robotId, flags);
        await _hub.Clients.Group(BackendV2.Api.SignalR.RealtimeGroups.Robots).SendCoreAsync(SignalRTopics.RobotConfigUpdated, new object[] { new { robotId, featureFlags = flags } }, System.Threading.CancellationToken.None);
    }

    public async Task PublishCurrentAsync(string robotId)
    {
        var session = await _db.RobotSessions.AsNoTracking().FirstOrDefaultAsync(x => x.RobotId == robotId);
        if (session == null) return;
        if (session.Connected)
        {
            var limits = SafeDeserialize<MotionLimitsDto>(session.MotionLimitsJson);
            var flags = SafeDeserialize<RobotFeatureFlagsDto>(session.FeatureFlagsJson);
            await _nats.PublishCfgMotionLimitsAsync(robotId, limits);
            await _nats.PublishCfgFeaturesAsync(robotId, flags);
            await _nats.PublishCfgRuntimeModeAsync(robotId, session.RuntimeMode);
        }
    }

    private static void ValidateLimits(MotionLimitsDto limits)
    {
        if (limits.MaxLinearVel < 0 || limits.MaxAngularVel < 0 || limits.MaxAccel < 0 || limits.MaxDecel < 0) throw new InvalidOperationException("Limits must be non-negative");
    }

    private static T SafeDeserialize<T>(string json) where T : new()
    {
        try { return JsonSerializer.Deserialize<T>(json) ?? new T(); }
        catch { return new T(); }
    }
}

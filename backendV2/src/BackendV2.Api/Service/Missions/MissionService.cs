using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using BackendV2.Api.Dto.Missions;
using BackendV2.Api.Dto.Robots;
using BackendV2.Api.Infrastructure.Persistence;
using BackendV2.Api.Model.Task;
using BackendV2.Api.Topics;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace BackendV2.Api.Service.Missions;

public class MissionService
{
    private readonly AppDbContext _db;
    private readonly IHubContext<BackendV2.Api.Hub.RealtimeHub> _hub;
    public MissionService(AppDbContext db, IHubContext<BackendV2.Api.Hub.RealtimeHub> hub)
    {
        _db = db;
        _hub = hub;
    }

    public async Task<Mission> CreateAsync(MissionCreateRequest req, Guid? actorUserId)
    {
        var m = new Mission { MissionId = Guid.NewGuid(), Name = req.Name, Version = 1, CreatedBy = actorUserId, CreatedAt = DateTimeOffset.UtcNow, StepsJson = JsonSerializer.Serialize(req.Steps) };
        await _db.Missions.AddAsync(m);
        await _db.SaveChangesAsync();
        await _hub.Clients.Group(BackendV2.Api.SignalR.RealtimeGroups.Robots).SendCoreAsync(SignalRTopics.MissionCreated, new object[] { new { missionId = m.MissionId.ToString(), name = m.Name } }, System.Threading.CancellationToken.None);
        return m;
    }

    public async Task<Mission> UpdateAsync(Guid missionId, MissionUpdateRequest req)
    {
        var m = await _db.Missions.FirstOrDefaultAsync(x => x.MissionId == missionId) ?? throw new InvalidOperationException("Mission not found");
        m.Name = req.Name;
        m.StepsJson = JsonSerializer.Serialize(req.Steps);
        await _db.SaveChangesAsync();
        await _hub.Clients.Group(BackendV2.Api.SignalR.RealtimeGroups.Robots).SendCoreAsync(SignalRTopics.MissionUpdated, new object[] { new { missionId = m.MissionId.ToString(), name = m.Name } }, System.Threading.CancellationToken.None);
        return m;
    }

    public async Task<bool> ValidateAsync(Guid missionId, string robotId)
    {
        var m = await _db.Missions.AsNoTracking().FirstOrDefaultAsync(x => x.MissionId == missionId) ?? throw new InvalidOperationException("Mission not found");
        var session = await _db.RobotSessions.AsNoTracking().FirstOrDefaultAsync(x => x.RobotId == robotId) ?? throw new InvalidOperationException("Robot session not found");
        var caps = SafeDeserialize<RobotCapabilitiesDto>(session.CapabilitiesJson);
        var flags = SafeDeserialize<RobotFeatureFlagsDto>(session.FeatureFlagsJson);
        var steps = SafeDeserialize<List<MissionStepDto>>(m.StepsJson);
        foreach (var step in steps)
        {
            if (string.Equals(step.Action, "ROTATE", StringComparison.OrdinalIgnoreCase) && !caps.SupportsRotate) return false;
            if (step.Action.StartsWith("TELESCOPE", StringComparison.OrdinalIgnoreCase) && (!caps.SupportsTelescope || !flags.TelescopeEnabled)) return false;
        }
        return true;
    }

    private static T SafeDeserialize<T>(string json) where T : new()
    {
        try { return JsonSerializer.Deserialize<T>(json) ?? new T(); }
        catch { return new T(); }
    }
}

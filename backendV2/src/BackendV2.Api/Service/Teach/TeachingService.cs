using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using BackendV2.Api.Contracts.State;
using BackendV2.Api.Dto.Missions;
using BackendV2.Api.Dto.Teach;
using BackendV2.Api.Infrastructure.Persistence;
using BackendV2.Api.Model.Task;
using BackendV2.Api.Topics;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace BackendV2.Api.Service.Teach;

public class TeachingService
{
    private readonly AppDbContext _db;
    private readonly IHubContext<BackendV2.Api.Hub.RealtimeHub> _hub;
    public TeachingService(AppDbContext db, IHubContext<BackendV2.Api.Hub.RealtimeHub> hub)
    {
        _db = db;
        _hub = hub;
    }

    public async global::System.Threading.Tasks.Task<TeachSession> CreateSessionAsync(TeachSessionCreateRequest req, Guid? actorUserId)
    {
        var ts = new TeachSession { TeachSessionId = Guid.NewGuid(), RobotId = req.RobotId, MapVersionId = req.MapVersionId, CreatedBy = actorUserId, Status = "CREATED", CreatedAt = DateTimeOffset.UtcNow, CapturedStepsJson = "[]" };
        await _db.TeachSessions.AddAsync(ts);
        await _db.SaveChangesAsync();
        return ts;
    }

    public async global::System.Threading.Tasks.Task StartSessionAsync(Guid teachSessionId)
    {
        var s = await _db.TeachSessions.FirstOrDefaultAsync(x => x.TeachSessionId == teachSessionId) ?? throw new InvalidOperationException("Teach session not found");
        s.Status = "STARTED";
        s.StartedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();
        await _hub.Clients.Group(BackendV2.Api.SignalR.RealtimeGroups.Robots).SendCoreAsync(SignalRTopics.TeachSessionStarted, new object[] { new { teachSessionId = teachSessionId.ToString(), robotId = s.RobotId } }, System.Threading.CancellationToken.None);
    }

    public async global::System.Threading.Tasks.Task StopSessionAsync(Guid teachSessionId)
    {
        var s = await _db.TeachSessions.FirstOrDefaultAsync(x => x.TeachSessionId == teachSessionId) ?? throw new InvalidOperationException("Teach session not found");
        s.Status = "STOPPED";
        s.StoppedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();
        await _hub.Clients.Group(BackendV2.Api.SignalR.RealtimeGroups.Robots).SendCoreAsync(SignalRTopics.TeachSessionStopped, new object[] { new { teachSessionId = teachSessionId.ToString(), robotId = s.RobotId } }, System.Threading.CancellationToken.None);
    }

    public async global::System.Threading.Tasks.Task CaptureStepAsync(Guid teachSessionId, TeachCaptureRequest req)
    {
        var s = await _db.TeachSessions.FirstOrDefaultAsync(x => x.TeachSessionId == teachSessionId) ?? throw new InvalidOperationException("Teach session not found");
        var arr = string.IsNullOrWhiteSpace(s.CapturedStepsJson) ? new List<object>() : JsonSerializer.Deserialize<List<object>>(s.CapturedStepsJson) ?? new List<object>();
        arr.Add(new { correlationId = req.CorrelationId, state = req.RobotState });
        s.CapturedStepsJson = JsonSerializer.Serialize(arr);
        await _db.SaveChangesAsync();
        await _hub.Clients.Group(BackendV2.Api.SignalR.RealtimeGroups.Robots).SendCoreAsync(SignalRTopics.TeachStepCaptured, new object[] { new { teachSessionId = teachSessionId.ToString(), correlationId = req.CorrelationId } }, System.Threading.CancellationToken.None);
    }

    public async global::System.Threading.Tasks.Task<Mission> SaveMissionAsync(Guid teachSessionId, string name)
    {
        var s = await _db.TeachSessions.AsNoTracking().FirstOrDefaultAsync(x => x.TeachSessionId == teachSessionId) ?? throw new InvalidOperationException("Teach session not found");
        var captured = string.IsNullOrWhiteSpace(s.CapturedStepsJson) ? new List<object>() : JsonSerializer.Deserialize<List<object>>(s.CapturedStepsJson) ?? new List<object>();
        var steps = new List<MissionStepDto>();
        foreach (var item in captured)
        {
            steps.Add(new MissionStepDto { Action = "SET_STATE", Parameters = item });
        }
        var mission = new Mission { MissionId = Guid.NewGuid(), Name = name, Version = 1, CreatedAt = DateTimeOffset.UtcNow, StepsJson = JsonSerializer.Serialize(steps) };
        await _db.Missions.AddAsync(mission);
        await _db.SaveChangesAsync();
        await _hub.Clients.Group(BackendV2.Api.SignalR.RealtimeGroups.Robots).SendCoreAsync(SignalRTopics.MissionCreated, new object[] { new { missionId = mission.MissionId.ToString(), name = mission.Name } }, System.Threading.CancellationToken.None);
        return mission;
    }
}

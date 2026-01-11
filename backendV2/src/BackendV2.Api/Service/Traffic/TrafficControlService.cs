using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BackendV2.Api.Dto.Traffic;
using BackendV2.Api.Infrastructure.Persistence;
using BackendV2.Api.Model.Map;
using BackendV2.Api.Topics;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Text.Json;
using BackendV2.Api.Contracts.Telemetry;
using BackendV2.Api.Dto.Config;

namespace BackendV2.Api.Service.Traffic;

public class TrafficControlService
{
    private readonly AppDbContext _db;
    private readonly IHubContext<BackendV2.Api.Hub.RealtimeHub> _hub;
    public TrafficControlService(AppDbContext db, IHubContext<BackendV2.Api.Hub.RealtimeHub> hub)
    {
        _db = db;
        _hub = hub;
    }

    public async Task<List<object>> GetConflictsAsync()
    {
        return new List<object>();
    }

    public async Task<List<RobotScheduleSummaryDto>> ComputeScheduleSummariesAsync()
    {
        var robots = await _db.Robots.AsNoTracking().Where(r => r.Connected && r.State != "FAULT" && r.State != "E_STOP").ToListAsync();
        var tasks = await _db.Tasks.AsNoTracking().ToListAsync();
        var paths = await _db.Paths.AsNoTracking().ToListAsync();
        var sessions = await _db.RobotSessions.AsNoTracking().ToListAsync();
        var summaries = new List<RobotScheduleSummaryDto>();
        foreach (var r in robots)
        {
            var currentTask = tasks.FirstOrDefault(t => t.RobotId == r.RobotId && (t.Status == "ASSIGNED" || t.Status == "EXECUTING"));
            var targetVel = 1.0;
            double headway = 0;
            if (currentTask?.CurrentRouteId != null)
            {
                var routePaths = paths.Where(p => p.MapVersionId == currentTask.MapVersionId).ToList();
                var leadVel = routePaths.Average(p => p.SpeedLimit ?? 1.0);
                var minFollow = routePaths.Average(p => p.MinFollowingDistanceMeters ?? 0);
                headway = leadVel > 0 ? minFollow / leadVel : 0;
                targetVel = leadVel;
            }
            var session = sessions.FirstOrDefault(s => s.RobotId == r.RobotId);
            if (session != null && !string.IsNullOrWhiteSpace(session.MotionLimitsJson))
            {
                try
                {
                    var limits = JsonSerializer.Deserialize<MotionLimitsDto>(session.MotionLimitsJson);
                    if (limits != null && limits.MaxLinearVel > 0) targetVel = Math.Min(targetVel, limits.MaxLinearVel);
                }
                catch { }
            }
            var lastMotion = await _db.RobotEvents.AsNoTracking().Where(e => e.RobotId == r.RobotId && e.Type == "telemetry.motion").OrderByDescending(e => e.Timestamp).FirstOrDefaultAsync();
            if (lastMotion != null)
            {
                try
                {
                    var mt = JsonSerializer.Deserialize<MotionTelemetry>(lastMotion.Payload);
                    if (mt != null && mt.CurrentLinearVel >= 0)
                    {
                        if (mt.CurrentLinearVel + 0.1 < targetVel) targetVel = mt.CurrentLinearVel + 0.1;
                    }
                }
                catch { }
            }
            var lastRadar = await _db.RobotEvents.AsNoTracking().Where(e => e.RobotId == r.RobotId && e.Type == "telemetry.radar").OrderByDescending(e => e.Timestamp).FirstOrDefaultAsync();
            if (lastRadar != null)
            {
                try
                {
                    var rt = JsonSerializer.Deserialize<RadarTelemetry>(lastRadar.Payload);
                    if (rt != null && rt.ObstacleDetected && rt.Distance < 1.0) targetVel = 0.0;
                }
                catch { }
            }
            summaries.Add(new RobotScheduleSummaryDto { RobotId = r.RobotId, CurrentRouteId = currentTask?.CurrentRouteId?.ToString(), TargetLinearVel = targetVel, HeadwaySeconds = headway });
        }
        return summaries;
    }

    public async Task EmitScheduleSummaryAsync()
    {
        var summaries = await ComputeScheduleSummariesAsync();
        await _hub.Clients.Group(BackendV2.Api.SignalR.RealtimeGroups.Robots).SendCoreAsync(SignalRTopics.TrafficScheduleSummaryUpdated, new object[] { summaries }, System.Threading.CancellationToken.None);
    }
}

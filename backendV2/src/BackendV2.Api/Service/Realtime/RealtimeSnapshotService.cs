using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using BackendV2.Api.Dto.Core;
using BackendV2.Api.Dto.Fleet;
using BackendV2.Api.Dto.Traffic;
using BackendV2.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BackendV2.Api.Service.Realtime;

public class RealtimeSnapshotService
{
    private readonly AppDbContext _db;
    public RealtimeSnapshotService(AppDbContext db) { _db = db; }

    public async Task<FleetSummaryDto> GetFleetSummaryAsync()
    {
        var robots = await _db.Robots.CountAsync();
        var tasks = await _db.Tasks.CountAsync();
        var incidents = 0;
        return new FleetSummaryDto { Robots = robots, Tasks = tasks, Incidents = incidents };
    }

    public async Task<TrafficOverviewDto> GetTrafficOverviewAsync()
    {
        var holds = await _db.TrafficHolds.CountAsync();
        var robotsOnline = await _db.RobotSessions.CountAsync(x => x.Connected);
        var tasksActive = await _db.Tasks.CountAsync(x => x.Status == "EXECUTING");
        return new TrafficOverviewDto { ActiveHolds = holds, RobotsOnline = robotsOnline, TasksActive = tasksActive };
    }

    public async Task<RobotSessionDto?> GetRobotSessionAsync(string robotId)
    {
        var s = await _db.RobotSessions.FirstOrDefaultAsync(x => x.RobotId == robotId);
        if (s == null) return null;
        return new RobotSessionDto
        {
            RobotId = s.RobotId,
            Connected = s.Connected,
            LastSeen = s.LastSeen,
            RuntimeMode = s.RuntimeMode,
            SoftwareVersion = s.SoftwareVersion,
            Capabilities = JsonSerializer.Deserialize<object>(s.CapabilitiesJson) ?? new { },
            FeatureFlags = JsonSerializer.Deserialize<object>(s.FeatureFlagsJson) ?? new { }
        };
    }

    public async Task<RobotStateDto> GetRobotStateSnapshotAsync(string robotId)
    {
        var r = await _db.Robots.FirstOrDefaultAsync(x => x.RobotId == robotId);
        return new RobotStateDto
        {
            RobotId = robotId,
            Mode = r?.State ?? "IDLE",
            BatteryPct = (double)(r?.Battery ?? 0)
        };
    }

    public async Task<object> GetRobotTelemetrySnapshotAsync(string robotId)
    {
        var r = await _db.Robots.FirstOrDefaultAsync(x => x.RobotId == robotId);
        return new { robotId, battery = r?.Battery ?? 0.0, x = r?.Location?.X ?? (r?.X ?? 0), y = r?.Location?.Y ?? (r?.Y ?? 0) };
    }

    public async Task<object> GetTasksOverviewSnapshotAsync()
    {
        var total = await _db.Tasks.CountAsync();
        var executing = await _db.Tasks.CountAsync(x => x.Status == "EXECUTING");
        var paused = await _db.Tasks.CountAsync(x => x.Status == "PAUSED");
        var pending = await _db.Tasks.CountAsync(x => x.Status == "ASSIGNED" || x.Status == "CREATED");
        return new { total, executing, paused, pending };
    }

    public async Task<object> GetActiveMapSnapshotAsync()
    {
        var active = await _db.MapVersions.AsNoTracking().FirstOrDefaultAsync(x => x.IsActive);
        if (active == null) return new { hasActive = false };
        var nodes = await _db.Nodes.AsNoTracking().CountAsync(x => x.MapVersionId == active.MapVersionId);
        var paths = await _db.Paths.AsNoTracking().CountAsync(x => x.MapVersionId == active.MapVersionId);
        var points = await _db.Points.AsNoTracking().CountAsync(x => x.MapVersionId == active.MapVersionId);
        return new { hasActive = true, mapVersionId = active.MapVersionId, name = active.Name, version = active.Version, nodes, paths, points };
    }
}

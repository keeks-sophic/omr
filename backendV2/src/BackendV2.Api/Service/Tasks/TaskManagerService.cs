using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using BackendV2.Api.Contracts.Routes;
using BackendV2.Api.Contracts.Tasks;
using BackendV2.Api.Dto.Tasks;
using BackendV2.Api.Infrastructure.Persistence;
using BackendV2.Api.Mapping.Tasks;
using BackendV2.Api.Model.Map;
using BackendV2.Api.Model.Task;
using BackendV2.Api.Topics;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace BackendV2.Api.Service.Tasks;

public class TaskManagerService
{
    private readonly AppDbContext _db;
    private readonly IHubContext<BackendV2.Api.Hub.RealtimeHub> _hub;
    private readonly NatsPublisherStub _nats;
    public TaskManagerService(AppDbContext db, IHubContext<BackendV2.Api.Hub.RealtimeHub> hub, NatsPublisherStub nats)
    {
        _db = db;
        _hub = hub;
        _nats = nats;
    }

    public async global::System.Threading.Tasks.Task<Dto.Tasks.TaskDto> CreateAsync(TaskCreateRequest req, Guid? actorUserId)
    {
        string? robotId = req.RobotId;
        if (string.Equals(req.AssignmentMode, "AUTO", StringComparison.OrdinalIgnoreCase))
        {
            robotId ??= await AutoAssignAsync(req);
        }
        var taskId = Guid.NewGuid();
        var task = new BackendV2.Api.Model.Task.Task
        {
            TaskId = taskId,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = actorUserId,
            Priority = req.Priority,
            Status = "ASSIGNED",
            AssignmentMode = req.AssignmentMode,
            RobotId = robotId,
            MapVersionId = req.MapVersionId,
            TaskType = req.TaskType,
            ParametersJson = JsonSerializer.Serialize(req.Parameters),
            UpdatedAt = DateTimeOffset.UtcNow
        };
        await _db.Tasks.AddAsync(task);
        await _db.TaskEvents.AddAsync(new TaskEvent { TaskEventId = Guid.NewGuid(), TaskId = taskId, RobotId = robotId, Status = "CREATED", CreatedAt = DateTimeOffset.UtcNow });
        await _db.SaveChangesAsync();
        await _hub.Clients.Group(BackendV2.Api.SignalR.RealtimeGroups.Robots).SendAsync(SignalRTopics.TaskCreated, new { taskId = taskId.ToString(), robotId, taskType = req.TaskType });

        if (req.TaskType == "GO_TO_POINT")
        {
            var pointId = ExtractGuid(req.Parameters, "pointId");
            var point = await _db.Points.FirstOrDefaultAsync(p => p.PointId == pointId && p.MapVersionId == req.MapVersionId);
            if (point == null || !string.Equals(point.Type, "PICK_DROP", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Invalid pointId");
            var robot = robotId != null ? await _db.Robots.FirstOrDefaultAsync(r => r.RobotId == robotId) : null;
            var start = robot?.Location ?? new NetTopologySuite.Geometries.Point(robot?.X ?? 0, robot?.Y ?? 0) { SRID = 0 };
            var routeId = Guid.NewGuid();
            var route = new BackendV2.Api.Model.Task.Route
            {
                RouteId = routeId,
                MapVersionId = req.MapVersionId,
                CreatedAt = DateTimeOffset.UtcNow,
                Start = start,
                Goal = point.Location,
                SegmentsJson = "[]"
            };
            await _db.Routes.AddAsync(route);
            task.CurrentRouteId = routeId;
            task.UpdatedAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync();
            await _hub.Clients.Group(BackendV2.Api.SignalR.RealtimeGroups.Robots).SendAsync(SignalRTopics.TaskAssigned, new { taskId = taskId.ToString(), robotId });
            var assignment = new TaskAssignment { TaskId = taskId.ToString(), TaskType = req.TaskType, Parameters = req.Parameters, MissionId = task.MissionId?.ToString(), MapVersionId = req.MapVersionId.ToString() };
            var routeAssign = new RouteAssign { RouteId = routeId.ToString(), MapVersionId = req.MapVersionId.ToString(), Segments = new { } };
            if (!string.IsNullOrEmpty(robotId))
            {
                await _nats.PublishTaskAssignAsync(robotId, assignment);
                await _nats.PublishRouteAssignAsync(robotId, routeAssign);
            }
        }
        else if (req.TaskType == "CHARGE")
        {
            var pointId = ExtractGuid(req.Parameters, "pointId");
            var point = await _db.Points.FirstOrDefaultAsync(p => p.PointId == pointId && p.MapVersionId == req.MapVersionId);
            if (point == null || !string.Equals(point.Type, "CHARGE", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Invalid charge pointId");
            await _hub.Clients.Group(BackendV2.Api.SignalR.RealtimeGroups.Robots).SendAsync(SignalRTopics.TaskAssigned, new { taskId = taskId.ToString(), robotId });
            var assignment = new TaskAssignment { TaskId = taskId.ToString(), TaskType = req.TaskType, Parameters = req.Parameters, MissionId = task.MissionId?.ToString(), MapVersionId = req.MapVersionId.ToString() };
            if (!string.IsNullOrEmpty(robotId)) await _nats.PublishTaskAssignAsync(robotId, assignment);
        }
        else if (req.TaskType == "PICK_DROP")
        {
            var fromId = ExtractGuid(req.Parameters, "fromPointId");
            var toId = ExtractGuid(req.Parameters, "toPointId");
            var from = await _db.Points.FirstOrDefaultAsync(p => p.PointId == fromId && p.MapVersionId == req.MapVersionId);
            var to = await _db.Points.FirstOrDefaultAsync(p => p.PointId == toId && p.MapVersionId == req.MapVersionId);
            if (from == null || to == null || !string.Equals(from.Type, "PICK_DROP", StringComparison.OrdinalIgnoreCase) || !string.Equals(to.Type, "PICK_DROP", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Invalid pick/drop points");
            await _hub.Clients.Group(BackendV2.Api.SignalR.RealtimeGroups.Robots).SendAsync(SignalRTopics.TaskAssigned, new { taskId = taskId.ToString(), robotId });
            var assignment = new TaskAssignment { TaskId = taskId.ToString(), TaskType = req.TaskType, Parameters = req.Parameters, MissionId = task.MissionId?.ToString(), MapVersionId = req.MapVersionId.ToString() };
            if (!string.IsNullOrEmpty(robotId)) await _nats.PublishTaskAssignAsync(robotId, assignment);
        }
        else if (req.TaskType == "RUN_MISSION")
        {
            var missionId = ExtractGuid(req.Parameters, "missionId");
            var mission = await _db.Missions.AsNoTracking().FirstOrDefaultAsync(m => m.MissionId == missionId);
            if (mission == null) throw new InvalidOperationException("Mission not found");
            task.MissionId = missionId;
            task.UpdatedAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync();
            await _hub.Clients.Group(BackendV2.Api.SignalR.RealtimeGroups.Robots).SendAsync(SignalRTopics.TaskAssigned, new { taskId = taskId.ToString(), robotId });
            var assignment = new TaskAssignment { TaskId = taskId.ToString(), TaskType = req.TaskType, Parameters = req.Parameters, MissionId = missionId.ToString(), MapVersionId = req.MapVersionId.ToString() };
            if (!string.IsNullOrEmpty(robotId)) await _nats.PublishTaskAssignAsync(robotId, assignment);
        }
        return TaskMapper.ToDto(task);
    }

    public async global::System.Threading.Tasks.Task ControlAsync(Guid taskId, string control)
    {
        var task = await _db.Tasks.FirstOrDefaultAsync(t => t.TaskId == taskId);
        if (task == null) throw new InvalidOperationException("Task not found");
        if (control == "pause") task.Status = "PAUSED";
        else if (control == "resume") task.Status = "EXECUTING";
        else if (control == "cancel") task.Status = "CANCELLED";
        task.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.TaskEvents.AddAsync(new TaskEvent { TaskEventId = Guid.NewGuid(), TaskId = taskId, RobotId = task.RobotId, Status = control.ToUpperInvariant(), CreatedAt = DateTimeOffset.UtcNow });
        await _db.SaveChangesAsync();
        await _hub.Clients.Group(BackendV2.Api.SignalR.RealtimeGroups.Robots).SendAsync(SignalRTopics.TaskStatusChanged, new { taskId = taskId.ToString(), status = task.Status });
        if (!string.IsNullOrEmpty(task.RobotId))
        {
            await _nats.PublishTaskControlAsync(task.RobotId, taskId.ToString(), control);
        }
    }

    private static Guid ExtractGuid(object parameters, string property)
    {
        var json = JsonSerializer.Serialize(parameters);
        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.TryGetProperty(property, out var el) && el.ValueKind == JsonValueKind.String && Guid.TryParse(el.GetString(), out var g))
            return g;
        return Guid.Empty;
    }

    private async Task<string?> AutoAssignAsync(TaskCreateRequest req)
    {
        var robots = await _db.Robots.AsNoTracking().Where(r => r.Connected == true && (r.Battery ?? 0) >= 15 && r.State != "FAULT" && r.State != "E_STOP").ToListAsync();
        if (robots.Count == 0) return null;
        double congestionPenalty = await _db.TrafficHolds.CountAsync(h => h.MapVersionId == req.MapVersionId) * 5.0;
        double targetX = 0, targetY = 0;
        Guid targetPointId = Guid.Empty;
        if (req.TaskType == "GO_TO_POINT" || req.TaskType == "CHARGE")
        {
            targetPointId = ExtractGuid(req.Parameters, "pointId");
            var p = await _db.Points.AsNoTracking().FirstOrDefaultAsync(x => x.PointId == targetPointId);
            if (p != null) { targetX = p.Location.X; targetY = p.Location.Y; }
        }
        else if (req.TaskType == "PICK_DROP")
        {
            targetPointId = ExtractGuid(req.Parameters, "fromPointId");
            var p = await _db.Points.AsNoTracking().FirstOrDefaultAsync(x => x.PointId == targetPointId);
            if (p != null) { targetX = p.Location.X; targetY = p.Location.Y; }
        }
        string? best = null;
        double bestCost = double.MaxValue;
        foreach (var r in robots)
        {
            var rx = r.Location?.X ?? (r.X ?? 0);
            var ry = r.Location?.Y ?? (r.Y ?? 0);
            var dist = Math.Sqrt(Math.Pow(rx - targetX, 2) + Math.Pow(ry - targetY, 2));
            var cost = dist + congestionPenalty + (100 - (r.Battery ?? 0)) * 0.1;
            if (cost < bestCost) { bestCost = cost; best = r.RobotId; }
        }
        return best;
    }
}

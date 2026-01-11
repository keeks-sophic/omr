using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BackendV2.Api.Dto.Routes;
using BackendV2.Api.Infrastructure.Persistence;
using BackendV2.Api.Model.Core;
using BackendV2.Api.Model.Map;
using BackendV2.Api.Model.Task;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace BackendV2.Api.Service.Routes;

public class RoutePlannerService
{
    private readonly AppDbContext _db;
    public RoutePlannerService(AppDbContext db) { _db = db; }

    public async System.Threading.Tasks.Task<(BackendV2.Api.Model.Task.Route route, List<RouteSegmentDto> segments, DateTimeOffset? eta)> PlanAsync(RoutePlanRequest req)
    {
        var nodes = await _db.Nodes.Where(n => n.MapVersionId == req.MapVersionId && n.IsActive && !n.IsMaintenance).ToListAsync();
        var paths = await _db.Paths.Where(p => p.MapVersionId == req.MapVersionId && p.IsActive && !p.IsMaintenance).ToListAsync();
        var holds = await _db.TrafficHolds.Where(h => h.MapVersionId == req.MapVersionId && h.EndTime > DateTimeOffset.UtcNow).ToListAsync();
        var pathHoldCounts = holds.Where(h => h.PathId != null).GroupBy(h => h.PathId!.Value).ToDictionary(g => g.Key, g => g.Count());
        var nodeHoldCounts = holds.Where(h => h.NodeId != null).GroupBy(h => h.NodeId!.Value).ToDictionary(g => g.Key, g => g.Count());
        var point = await _db.Points.FirstOrDefaultAsync(p => p.PointId == req.GoalPointId && p.MapVersionId == req.MapVersionId);
        if (point == null) throw new InvalidOperationException("Goal point not found");
        if (point.AttachedNodeId == null)
        {
            point.AttachedNodeId = NearestNode(nodes, point.Location);
        }
        var startLoc = await ResolveStartAsync(req, nodes);
        var startNodeId = NearestNode(nodes, startLoc);
        var goalNodeId = point.AttachedNodeId!.Value;
        var pathInfos = ShortestPathWithCost(nodes, paths, pathHoldCounts, nodeHoldCounts, startNodeId, goalNodeId);
        if (pathInfos.Count == 0) throw new InvalidOperationException("Unreachable goal");
        var segs = new List<RouteSegmentDto>();
        double totalSeconds = 0;
        var maxRobotSpeed = 0.0;
        if (!string.IsNullOrEmpty(req.RobotId))
        {
            var session = await _db.RobotSessions.AsNoTracking().FirstOrDefaultAsync(x => x.RobotId == req.RobotId);
            if (session != null)
            {
                try
                {
                    var limits = System.Text.Json.JsonSerializer.Deserialize<BackendV2.Api.Dto.Config.MotionLimitsDto>(session.MotionLimitsJson);
                    if (limits != null && limits.MaxLinearVel > 0) maxRobotSpeed = limits.MaxLinearVel;
                }
                catch { }
            }
        }
        foreach (var info in pathInfos)
        {
            var p = paths.First(x => x.PathId == info.pathId);
            var speed = p.SpeedLimit ?? 1.0;
            if (speed <= 0) speed = 1.0;
            if (maxRobotSpeed > 0) speed = Math.Min(speed, maxRobotSpeed);
            var baseSeconds = p.Location.Length / speed;
            var holdPenalty = (pathHoldCounts.TryGetValue(p.PathId, out var ph) ? ph : 0) * 10.0;
            holdPenalty += (nodeHoldCounts.TryGetValue(p.FromNodeId, out var nf) ? nf : 0) * 5.0;
            holdPenalty += (nodeHoldCounts.TryGetValue(p.ToNodeId, out var nt) ? nt : 0) * 5.0;
            var segSeconds = baseSeconds + holdPenalty;
            segs.Add(new RouteSegmentDto { PathId = p.PathId, Direction = info.forward ? "FORWARD" : "REVERSE", EstimatedSeconds = segSeconds });
            totalSeconds += segSeconds;
        }
        var route = new BackendV2.Api.Model.Task.Route
        {
            RouteId = Guid.NewGuid(),
            MapVersionId = req.MapVersionId,
            CreatedAt = DateTimeOffset.UtcNow,
            Start = startLoc,
            Goal = point.Location,
            SegmentsJson = System.Text.Json.JsonSerializer.Serialize(segs)
        };
        route.EstimatedStartTime = DateTimeOffset.UtcNow;
        var eta = DateTimeOffset.UtcNow.AddSeconds(totalSeconds);
        route.EstimatedArrivalTime = eta;
        return (route, segs, eta);
    }

    private static Guid NearestNode(List<MapNode> nodes, Point target)
    {
        var nearest = nodes.OrderBy(n => n.Location.Distance(target)).FirstOrDefault();
        return nearest?.NodeId ?? Guid.Empty;
    }

    private async Task<Point> ResolveStartAsync(RoutePlanRequest req, List<MapNode> nodes)
    {
        if (req.StartX.HasValue && req.StartY.HasValue) return new Point(req.StartX.Value, req.StartY.Value) { SRID = 0 };
        if (!string.IsNullOrEmpty(req.RobotId))
        {
            var r = await _db.Robots.FirstOrDefaultAsync(x => x.RobotId == req.RobotId);
            if (r?.Location != null) return r.Location;
            if (r != null) return new Point(r.X ?? 0, r.Y ?? 0) { SRID = 0 };
        }
        var origin = nodes.FirstOrDefault()?.Location ?? new Point(0, 0) { SRID = 0 };
        return origin;
    }

    private static List<(Guid pathId, bool forward)> ShortestPathWithCost(List<MapNode> nodes, List<MapPath> paths, System.Collections.Generic.Dictionary<Guid, int> pathHoldCounts, System.Collections.Generic.Dictionary<Guid, int> nodeHoldCounts, Guid startNodeId, Guid goalNodeId)
    {
        var adj = new Dictionary<Guid, List<(Guid next, Guid pathId, double cost, bool forward)>>();
        foreach (var p in paths)
        {
            if (!adj.ContainsKey(p.FromNodeId)) adj[p.FromNodeId] = new List<(Guid, Guid, double, bool)>();
            var speed = p.SpeedLimit ?? 1.0;
            if (speed <= 0) speed = 1.0;
            var baseCost = p.Location.Length / speed;
            var penalty = (pathHoldCounts.TryGetValue(p.PathId, out var ph) ? ph : 0) * 10.0;
            penalty += (nodeHoldCounts.TryGetValue(p.FromNodeId, out var nf) ? nf : 0) * 5.0;
            penalty += (nodeHoldCounts.TryGetValue(p.ToNodeId, out var nt) ? nt : 0) * 5.0;
            adj[p.FromNodeId].Add((p.ToNodeId, p.PathId, baseCost + penalty, true));
            if (p.TwoWay)
            {
                if (!adj.ContainsKey(p.ToNodeId)) adj[p.ToNodeId] = new List<(Guid, Guid, double, bool)>();
                adj[p.ToNodeId].Add((p.FromNodeId, p.PathId, baseCost + penalty, false));
            }
        }
        var dist = new Dictionary<Guid, double>();
        var prev = new Dictionary<Guid, (Guid node, Guid pathId, bool forward)>();
        var unvisited = new HashSet<Guid>(nodes.Select(n => n.NodeId));
        foreach (var n in unvisited) dist[n] = double.PositiveInfinity;
        if (!unvisited.Contains(startNodeId) || !unvisited.Contains(goalNodeId)) return new List<(Guid, bool)>();
        dist[startNodeId] = 0;
        while (unvisited.Count > 0)
        {
            var u = unvisited.OrderBy(n => dist[n]).First();
            unvisited.Remove(u);
            if (u == goalNodeId) break;
            if (!adj.ContainsKey(u)) continue;
            foreach (var (next, pathId, cost, forward) in adj[u])
            {
                if (!unvisited.Contains(next)) continue;
                var alt = dist[u] + cost;
                if (alt < dist[next])
                {
                    dist[next] = alt;
                    prev[next] = (u, pathId, forward);
                }
            }
        }
        var pathInfos = new List<(Guid pathId, bool forward)>();
        var cur = goalNodeId;
        while (cur != startNodeId && prev.ContainsKey(cur))
        {
            var p = prev[cur];
            pathInfos.Insert(0, (p.pathId, p.forward));
            cur = p.node;
        }
        return pathInfos;
    }
}

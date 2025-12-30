using Backend.Infrastructure.Persistence;
using Backend.Topics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetTopologySuite.Geometries;
using NetTopologySuite.LinearReferencing;
using Microsoft.AspNetCore.SignalR;
using Backend.SignalR;
using Backend.Service;
using Backend.Topics;

namespace Backend.Worker;

public class RoutePlannerWorker : BackgroundService
{
    private readonly ILogger<RoutePlannerWorker> _logger;
    private readonly IServiceProvider _sp;
    private readonly IRoutePlanQueue _queue;
    private readonly NatsService _nats;
    private readonly IOptions<NatsOptions> _opts;
    private readonly IHubContext<RobotsHub> _hub;

    public RoutePlannerWorker(ILogger<RoutePlannerWorker> logger, IServiceProvider sp, IRoutePlanQueue queue, NatsService nats, IOptions<NatsOptions> opts, IHubContext<RobotsHub> hub)
    {
        _logger = logger;
        _sp = sp;
        _queue = queue;
        _nats = nats;
        _opts = opts;
        _hub = hub;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var task in _queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = _sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var robot = await db.Robots.AsNoTracking().FirstOrDefaultAsync(r => r.Ip == task.Ip, stoppingToken);
                var startNodeId = await ResolveNearestEndpointNodeIdAsync(db, task.MapId, robot?.Location != null ? robot.Location.X : (robot?.X ?? 0), robot?.Location != null ? robot.Location.Y : (robot?.Y ?? 0), stoppingToken);
                var destNodeId = await ResolveNearestEndpointNodeIdAsync(db, task.MapId, task.X, task.Y, stoppingToken);
                if (startNodeId == null || destNodeId == null)
                {
                    _logger.LogWarning("Route plan failed: start or destination node not found");
                    continue;
                }
                var repo = scope.ServiceProvider.GetRequiredService<Backend.Data.MapRepository>();
                var pgRoute = await repo.TryComputeRouteWithPgRoutingAsync(task.MapId, startNodeId.Value, destNodeId.Value, stoppingToken);
                var (nodeIds, pathIds, totalLength) = pgRoute ?? ComputeRoute(db, task.MapId, startNodeId.Value, destNodeId.Value);
                var dbNodes = await db.Nodes.AsNoTracking().Where(n => nodeIds.Contains(n.Id)).Select(n => new { n.Id, n.X, n.Y, n.Location }).ToListAsync(stoppingToken);
                var coords = new List<object>();
                var startX = robot?.Location != null ? robot.Location.X : (robot?.X ?? 0);
                var startY = robot?.Location != null ? robot.Location.Y : (robot?.Y ?? 0);
                var pathEntities = await db.Paths.AsNoTracking().Where(p => pathIds.Contains(p.Id)).Select(p => new { p.Id, p.Location, p.StartNodeId, p.EndNodeId }).ToListAsync(stoppingToken);
                var hasValidPath = pathEntities.Count > 0 && nodeIds.Length > 1;
                var addedSamples = false;
                if (hasValidPath)
                {
                    var firstPath = pathEntities.FirstOrDefault(p => p.StartNodeId == nodeIds[0] && p.EndNodeId == nodeIds[1]) ?? pathEntities.First();
                    if (firstPath.Location is LineString line1)
                    {
                        var lir = new LengthIndexedLine(line1);
                        var idx = lir.Project(new Coordinate(startX, startY));
                        var c = lir.ExtractPoint(idx);
                        startX = c.X;
                        startY = c.Y;
                        coords.Add(new { id = 0, x = startX, y = startY });
                    }
                }
                var destX = task.X;
                var destY = task.Y;
                double sampleStep = 0.5;
                for (var i = 0; i + 1 < nodeIds.Length; i++)
                {
                    var aId = nodeIds[i];
                    var bId = nodeIds[i + 1];
                    var segPath = pathEntities.FirstOrDefault(p => p.StartNodeId == aId && p.EndNodeId == bId)
                        ?? pathEntities.FirstOrDefault(p => p.StartNodeId == bId && p.EndNodeId == aId);
                    if (segPath?.Location is LineString line)
                    {
                        var lir = new LengthIndexedLine(line);
                        double startIdx;
                        double endIdx;
                        if (i == 0)
                        {
                            startIdx = lir.Project(new Coordinate(startX, startY));
                        }
                        else
                        {
                            var aNode = dbNodes.First(n => n.Id == aId);
                            var ax = aNode.Location != null ? aNode.Location.X : aNode.X;
                            var ay = aNode.Location != null ? aNode.Location.Y : aNode.Y;
                            startIdx = lir.Project(new Coordinate(ax, ay));
                        }
                        if (i == nodeIds.Length - 2)
                        {
                            endIdx = lir.Project(new Coordinate(destX, destY));
                        }
                        else
                        {
                            var bNode = dbNodes.First(n => n.Id == bId);
                            var bx = bNode.Location != null ? bNode.Location.X : bNode.X;
                            var by = bNode.Location != null ? bNode.Location.Y : bNode.Y;
                            endIdx = lir.Project(new Coordinate(bx, by));
                        }
                        var reverse = endIdx < startIdx;
                        var length = Math.Abs(endIdx - startIdx);
                        var steps = Math.Max(1, (int)Math.Floor(length / sampleStep));
                        for (var s = 1; s <= steps; s++)
                        {
                            var dist = reverse ? -Math.Min(length, s * sampleStep) : Math.Min(length, s * sampleStep);
                            var idx = startIdx + dist;
                            var c = lir.ExtractPoint(idx);
                            coords.Add(new { id = bId, x = c.X, y = c.Y });
                            addedSamples = true;
                        }
                    }
                }
                if (hasValidPath)
                {
                    var lastPath = pathEntities.FirstOrDefault(p => p.StartNodeId == nodeIds[^2] && p.EndNodeId == nodeIds[^1]) ?? pathEntities.Last();
                    if (lastPath.Location is LineString line2)
                    {
                        var lir2 = new LengthIndexedLine(line2);
                        var idx2 = lir2.Project(new Coordinate(destX, destY));
                        var c2 = lir2.ExtractPoint(idx2);
                        destX = c2.X;
                        destY = c2.Y;
                    }
                }
                if (addedSamples)
                {
                    coords.Add(new { id = -1, x = destX, y = destY });
                }
                else
                {
                    coords.Clear();
                }
                var route = new
                {
                    command = NatsTopics.CommandRoutePlan,
                    ip = task.Ip,
                    route = new
                    {
                        mapId = task.MapId,
                        nodes = coords.ToArray(),
                        pathIds,
                        nodeIds,
                        length = totalLength,
                        speed = 2.0
                    }
                };
                var rid = robot?.Id ?? 0;
                await _nats.PublishAsync($"{NatsTopics.RoutePlanPrefix}.{rid}", route, stoppingToken);
                try
                {
                    await _hub.Clients.Group($"map:{task.MapId}").SendAsync(SignalRTopics.Route, new
                    {
                        ip = task.Ip,
                        mapId = task.MapId,
                        nodes = coords.ToArray(),
                        pathIds,
                        length = totalLength
                    }, stoppingToken);
                }
                catch { }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Route planning failed");
            }
        }
    }

    private async Task<int?> ResolveNearestEndpointNodeIdAsync(AppDbContext db, int mapId, double x, double y, CancellationToken ct)
    {
        var active = await db.Paths.AsNoTracking().Where(p => p.MapId == mapId && p.Status.ToLower() == "active").ToListAsync(ct);
        if (active.Count == 0) return await GetNearestNodeIdAsync(db, mapId, x, y, ct);
        var nodeIds = active.SelectMany(p => new[] { p.StartNodeId, p.EndNodeId }).Distinct().ToArray();
        var nodesMap = await db.Nodes.AsNoTracking().Where(n => nodeIds.Contains(n.Id)).ToDictionaryAsync(n => n.Id, ct);
        double best = double.MaxValue;
        Backend.Model.Paths? bestPath = null;
        foreach (var p in active)
        {
            if (!nodesMap.TryGetValue(p.StartNodeId, out var a) || !nodesMap.TryGetValue(p.EndNodeId, out var b)) continue;
            var ax = a.Location != null ? a.Location.X : a.X;
            var ay = a.Location != null ? a.Location.Y : a.Y;
            var bx = b.Location != null ? b.Location.X : b.X;
            var by = b.Location != null ? b.Location.Y : b.Y;
            var dist = DistanceToSegment(x, y, ax, ay, bx, by);
            if (dist < best)
            {
                best = dist;
                bestPath = p;
            }
        }
        if (bestPath == null) return await GetNearestNodeIdAsync(db, mapId, x, y, ct);
        var aNode = nodesMap[bestPath.StartNodeId];
        var bNode = nodesMap[bestPath.EndNodeId];
        var dax = Math.Abs((aNode.Location?.X ?? aNode.X) - x);
        var day = Math.Abs((aNode.Location?.Y ?? aNode.Y) - y);
        var dbx = Math.Abs((bNode.Location?.X ?? bNode.X) - x);
        var dby = Math.Abs((bNode.Location?.Y ?? bNode.Y) - y);
        var from = (dax + day) <= (dbx + dby) ? bestPath.StartNodeId : bestPath.EndNodeId;
        return from;
    }

    private static double DistanceToSegment(double px, double py, double ax, double ay, double bx, double by)
    {
        var vx = bx - ax;
        var vy = by - ay;
        var wx = px - ax;
        var wy = py - ay;
        var c1 = vx * wx + vy * wy;
        if (c1 <= 0) return Math.Sqrt(Math.Pow(px - ax, 2) + Math.Pow(py - ay, 2));
        var c2 = vx * vx + vy * vy;
        if (c2 <= c1) return Math.Sqrt(Math.Pow(px - bx, 2) + Math.Pow(py - by, 2));
        var t = c1 / c2;
        var projx = ax + t * vx;
        var projy = ay + t * vy;
        return Math.Sqrt(Math.Pow(px - projx, 2) + Math.Pow(py - projy, 2));
    }

    private async Task<int?> GetNearestNodeIdAsync(AppDbContext db, int mapId, double x, double y, CancellationToken ct)
    {
        var nodes = await db.Nodes.AsNoTracking().Where(n => n.MapId == mapId).Select(n => new { n.Id, n.X, n.Y, n.Location }).ToArrayAsync(ct);
        if (nodes.Length == 0) return null;
        var best = nodes.Select(n =>
        {
            var nx = n.Location != null ? n.Location.X : n.X;
            var ny = n.Location != null ? n.Location.Y : n.Y;
            var d = Math.Sqrt(Math.Pow(nx - x, 2) + Math.Pow(ny - y, 2));
            return (n.Id, d);
        }).OrderBy(t => t.d).First();
        return best.Id;
    }

    private static (int[] nodeIds, int[] pathIds, double totalLength) ComputeRoute(AppDbContext db, int mapId, int startNodeId, int destNodeId)
    {
        var paths = db.Paths.AsNoTracking().Where(p => p.MapId == mapId && p.Status.ToLower() == "active").ToArray();
        var adj = BuildAdjacency(paths);
        var nodePath = Dijkstra(startNodeId, destNodeId, adj) ?? Array.Empty<int>();
        var pathIds = ToPathIds(paths, nodePath);
        var length = ComputeLength(paths, nodePath);
        return (nodePath, pathIds, length);
    }

    private static Dictionary<int, List<(int to, double w)>> BuildAdjacency(Backend.Model.Paths[] paths)
    {
        var adj = new Dictionary<int, List<(int to, double w)>>();
        void AddEdge(int a, int b, double w)
        {
            if (!adj.TryGetValue(a, out var list)) { list = new List<(int to, double w)>(); adj[a] = list; }
            list.Add((b, w));
        }
        foreach (var p in paths)
        {
            var w = p.Location != null ? p.Location.Length : p.Length;
            AddEdge(p.StartNodeId, p.EndNodeId, w);
            if (p.TwoWay) AddEdge(p.EndNodeId, p.StartNodeId, w);
        }
        return adj;
    }

    private static int[]? Dijkstra(int start, int dest, Dictionary<int, List<(int to, double w)>> adj)
    {
        var dist = new Dictionary<int, double>();
        var prev = new Dictionary<int, int>();
        var pq = new PriorityQueue<int, double>();
        foreach (var v in adj.Keys) dist[v] = double.PositiveInfinity;
        dist[start] = 0.0;
        pq.Enqueue(start, 0.0);
        var visited = new HashSet<int>();
        while (pq.Count > 0)
        {
            var u = pq.Dequeue();
            if (!visited.Add(u)) continue;
            if (u == dest) break;
            if (!adj.TryGetValue(u, out var edges)) continue;
            foreach (var (v, w) in edges)
            {
                var alt = dist[u] + w;
                if (!dist.ContainsKey(v) || alt < dist[v])
                {
                    dist[v] = alt;
                    prev[v] = u;
                    pq.Enqueue(v, alt);
                }
            }
        }
        if (!prev.ContainsKey(dest) && start != dest) return null;
        var path = new List<int> { dest };
        var cur = dest;
        while (prev.ContainsKey(cur))
        {
            cur = prev[cur];
            path.Add(cur);
        }
        path.Reverse();
        return path.ToArray();
    }

    private static int[] ToPathIds(Backend.Model.Paths[] paths, int[] nodeIds)
    {
        var res = new List<int>();
        for (var i = 0; i + 1 < nodeIds.Length; i++)
        {
            var a = nodeIds[i];
            var b = nodeIds[i + 1];
            var p = paths.FirstOrDefault(x => x.StartNodeId == a && x.EndNodeId == b)
                ?? paths.FirstOrDefault(x => x.TwoWay && x.StartNodeId == b && x.EndNodeId == a);
            if (p != null) res.Add(p.Id);
        }
        return res.ToArray();
    }

    private static double ComputeLength(Backend.Model.Paths[] paths, int[] nodeIds)
    {
        var sum = 0.0;
        for (var i = 0; i + 1 < nodeIds.Length; i++)
        {
            var a = nodeIds[i];
            var b = nodeIds[i + 1];
            var p = paths.FirstOrDefault(x => x.StartNodeId == a && x.EndNodeId == b)
                ?? paths.FirstOrDefault(x => x.TwoWay && x.StartNodeId == b && x.EndNodeId == a);
            if (p is not null)
            {
                var segLen = p.Location != null ? p.Location.Length : p.Length;
                sum += segLen;
            }
        }
        return sum;
    }
}

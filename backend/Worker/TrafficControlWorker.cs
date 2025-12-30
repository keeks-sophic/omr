using System.Text;
using System.Text.Json;
using Backend.Infrastructure.Persistence;
using Backend.Topics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetTopologySuite.Geometries;
using NetTopologySuite.LinearReferencing;
using Backend.Service;
using Backend.Topics;
using NATS.Client;
using System.Collections.Concurrent;

namespace Backend.Worker;

public class TrafficControlWorker : BackgroundService
{
    private readonly ILogger<TrafficControlWorker> _logger;
    private readonly IServiceProvider _sp;
    private readonly NatsService _nats;
    private readonly IOptions<NatsOptions> _opts;
    private readonly object _lock = new();

    private readonly Dictionary<int, Dictionary<int, string>> _nodeOccupancyByMap = new();
    private readonly Dictionary<int, Dictionary<(int from, int to), string>> _edgeOccupancyByMap = new();
    private readonly Dictionary<string, (int? nodeId, (int from, int to)? edge)> _robotLast = new();
    private readonly ConcurrentDictionary<string, RobotSnap> _latest = new();

    public TrafficControlWorker(ILogger<TrafficControlWorker> logger, IServiceProvider sp, NatsService nats, IOptions<NatsOptions> opts)
    {
        _logger = logger;
        _sp = sp;
        _nats = nats;
        _opts = opts;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _ = _nats.ConnectAsync(_opts.Value.Url, stoppingToken);
        _ = _nats.EnsureStreamAsync(_opts.Value.RouteWildcardStream ?? "ROBOTS_ROUTE", $"{NatsTopics.RouteSegmentPrefix}.>");
        _ = _nats.EnsureStreamAsync(_opts.Value.ControlWildcardStream ?? "ROBOTS_CONTROL", $"{NatsTopics.ControlPrefix}.>");
        _ = _nats.EnsureStreamAsync(_opts.Value.TelemetryWildcardStream ?? "ROBOTS_TELEMETRY", $"{NatsTopics.TelemetryPrefix}.>");
        _nats.Subscribe($"{NatsTopics.RouteSegmentPrefix}.>", async (s, e) =>
        {
            await HandleRouteSegmentMessage(e, stoppingToken);
        });
        _nats.Subscribe($"{NatsTopics.TelemetryPrefix}.>", (s, e) =>
        {
            try
            {
                var payload = e.Message.Data != null ? Encoding.UTF8.GetString(e.Message.Data) : "{}";
                var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(payload) ? "{}" : payload);
                var ip = doc.RootElement.TryGetProperty("Ip", out var ipEl) ? ipEl.GetString() : (doc.RootElement.TryGetProperty("ip", out var ip2) ? ip2.GetString() : null);
                if (string.IsNullOrWhiteSpace(ip)) return;
                double? x = doc.RootElement.TryGetProperty("X", out var xEl) && xEl.ValueKind == JsonValueKind.Number ? xEl.GetDouble() : (doc.RootElement.TryGetProperty("x", out var x2) && x2.ValueKind == JsonValueKind.Number ? x2.GetDouble() : (double?)null);
                double? y = doc.RootElement.TryGetProperty("Y", out var yEl) && yEl.ValueKind == JsonValueKind.Number ? yEl.GetDouble() : (doc.RootElement.TryGetProperty("y", out var y2) && y2.ValueKind == JsonValueKind.Number ? y2.GetDouble() : (double?)null);
                var state = doc.RootElement.TryGetProperty("State", out var sEl) ? sEl.GetString() : (doc.RootElement.TryGetProperty("state", out var s2) ? s2.GetString() : null);
                int? mapId = doc.RootElement.TryGetProperty("MapId", out var mEl) && mEl.ValueKind == JsonValueKind.Number ? (int?)mEl.GetInt32() : (doc.RootElement.TryGetProperty("mapId", out var m2) && m2.ValueKind == JsonValueKind.Number ? (int?)m2.GetInt32() : null);
                var snap = new RobotSnap { Ip = ip!, X = x ?? 0, Y = y ?? 0, State = state, MapId = mapId, Last = DateTime.UtcNow };
                _latest[ip!] = snap;
            }
            catch { }
        });
        _logger.LogInformation("Traffic control worker started");
        return Task.CompletedTask;
    }

    private async Task HandleRouteSegmentMessage(MsgHandlerEventArgs e, CancellationToken stoppingToken)
    {
        try
        {
            var text = e.Message.Data != null ? Encoding.UTF8.GetString(e.Message.Data) : "{}";
            var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(text) ? "{}" : text);
            var command = doc.RootElement.TryGetProperty("command", out var cmd) ? cmd.GetString() : null;
            var ip = doc.RootElement.TryGetProperty("ip", out var ipEl) ? ipEl.GetString() : null;
            if (string.IsNullOrWhiteSpace(command) || string.IsNullOrWhiteSpace(ip)) return;
            if (!string.Equals(command, NatsTopics.CommandRouteSegment, StringComparison.OrdinalIgnoreCase)) return;
            var routeEl = doc.RootElement.TryGetProperty("segment", out var seg) ? seg : default;
            if (routeEl.ValueKind != JsonValueKind.Object) return;
            if (!routeEl.TryGetProperty("mapId", out var mapEl) || mapEl.ValueKind != JsonValueKind.Number) return;
            var mapId = mapEl.GetInt32();
            if (!routeEl.TryGetProperty("points", out var ptsEl) || ptsEl.ValueKind != JsonValueKind.Array) return;
            var segLen = routeEl.TryGetProperty("length", out var lenEl) && lenEl.ValueKind == JsonValueKind.Number ? lenEl.GetDouble() : 5.0;
            var pts = ptsEl.EnumerateArray().Select(p =>
            {
                var x = p.TryGetProperty("x", out var xx) && xx.ValueKind == JsonValueKind.Number ? xx.GetDouble() : 0;
                var y = p.TryGetProperty("y", out var yy) && yy.ValueKind == JsonValueKind.Number ? yy.GetDouble() : 0;
                return (x, y);
            }).ToList();
            if (pts.Count == 0) return;

            int? startNodeId;
            int? endNodeId;
            using (var scope = _sp.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var robotEntity = await db.Robots.AsNoTracking().FirstOrDefaultAsync(r => r.Ip == ip, stoppingToken);
                var robotId = robotEntity?.Id ?? 0;
                var firstPt = pts.First();
                var lastPt = pts.Last();
                startNodeId = await NearestNodeIdAsync(db, mapId, firstPt.x, firstPt.y, stoppingToken);
                endNodeId = await NearestNodeIdAsync(db, mapId, lastPt.x, lastPt.y, stoppingToken);
                if (startNodeId == null || endNodeId == null)
                {
                    await SendAllowAsync(ip!, robotId, true, segLen, stoppingToken);
                    return;
                }
                var path = await db.Paths.AsNoTracking()
                    .FirstOrDefaultAsync(p => p.MapId == mapId && p.StartNodeId == startNodeId && p.EndNodeId == endNodeId, stoppingToken)
                    ?? await db.Paths.AsNoTracking()
                        .FirstOrDefaultAsync(p => p.MapId == mapId && p.TwoWay && p.StartNodeId == endNodeId && p.EndNodeId == startNodeId, stoppingToken);
                if (path == null)
                {
                    var active = await db.Paths.AsNoTracking().Where(p => p.MapId == mapId && p.Status.ToLower() == "active").ToListAsync(stoppingToken);
                    var nodeIds = active.SelectMany(p => new[] { p.StartNodeId, p.EndNodeId }).Distinct().ToArray();
                    var nodesMap = await db.Nodes.AsNoTracking().Where(n => nodeIds.Contains(n.Id)).ToDictionaryAsync(n => n.Id, stoppingToken);
                    double best = double.MaxValue;
                    Backend.Model.Paths? bestPath = null;
                    foreach (var pth in active)
                    {
                        if (!nodesMap.TryGetValue(pth.StartNodeId, out var a) || !nodesMap.TryGetValue(pth.EndNodeId, out var b)) continue;
                        var ax = a.Location != null ? a.Location.X : a.X;
                        var ay = a.Location != null ? a.Location.Y : a.Y;
                        var bx = b.Location != null ? b.Location.X : b.X;
                        var by = b.Location != null ? b.Location.Y : b.Y;
                        var dist = DistanceToSegment(firstPt.x, firstPt.y, ax, ay, bx, by);
                        if (dist < best)
                        {
                            best = dist;
                            bestPath = pth;
                        }
                    }
                    path = bestPath;
                    if (path != null)
                    {
                        var a = nodesMap[path.StartNodeId];
                        var b = nodesMap[path.EndNodeId];
                        var dax = Math.Abs((a.Location?.X ?? a.X) - firstPt.x);
                        var dbx = Math.Abs((b.Location?.X ?? b.X) - firstPt.x);
                        var day = Math.Abs((a.Location?.Y ?? a.Y) - firstPt.y);
                        var dby = Math.Abs((b.Location?.Y ?? b.Y) - firstPt.y);
                        var from = (dax + day) <= (dbx + dby) ? path.StartNodeId : path.EndNodeId;
                        var to = from == path.StartNodeId ? path.EndNodeId : path.StartNodeId;
                        startNodeId = from;
                        endNodeId = to;
                    }
                }

                var endNode = await db.Nodes.AsNoTracking().FirstOrDefaultAsync(n => n.Id == endNodeId, stoppingToken);
                var endDist = double.MaxValue;
                if (endNode != null)
                {
                    var ex = endNode.Location != null ? endNode.Location.X : endNode.X;
                    var ey = endNode.Location != null ? endNode.Location.Y : endNode.Y;
                    var dxn = lastPt.x - ex;
                    var dyn = lastPt.y - ey;
                    endDist = Math.Sqrt(dxn * dxn + dyn * dyn);
                }
                var nearRobots = _latest.Values
                    .Where(r => r.MapId == mapId && r.Ip != ip && (DateTime.UtcNow - r.Last) <= TimeSpan.FromSeconds(5))
                    .Select(r => new { r.Ip, X = (double?)r.X, Y = (double?)r.Y, r.State })
                    .ToList();
                var line = path?.Location as LineString;
                double aheadLimit = segLen;
                if (line != null)
                {
                    var lir = new LengthIndexedLine(line);
                    var startIdx = lir.Project(new Coordinate(firstPt.x, firstPt.y));
                    var nearestAhead = nearRobots
                        .Select(r =>
                        {
                            var rx = r.X ?? 0;
                            var ry = r.Y ?? 0;
                            var ridx = lir.Project(new Coordinate(rx, ry));
                            var proj = lir.ExtractPoint(ridx);
                            var dx = rx - proj.X;
                            var dy = ry - proj.Y;
                            var perpDist = Math.Sqrt(dx * dx + dy * dy);
                            var st = (r.State ?? "").ToLowerInvariant();
                            var isBlockingState = st.Contains("idle") || st.Contains("stop");
                            return (ridx, ip2: r.Ip, perpDist, isBlockingState);
                        })
                        .Where(t => t.ridx > startIdx && t.isBlockingState && t.perpDist <= 0.3)
                        .OrderBy(t => t.ridx)
                        .FirstOrDefault();
                    if (nearestAhead.ip2 != null)
                    {
                        var delta = nearestAhead.ridx - startIdx;
                        if (delta <= 1.0)
                        {
                            aheadLimit = 0.0;
                        }
                        else
                        {
                            aheadLimit = Math.Max(0, delta - 1.0);
                        }
                    }
                }
                bool allowed;
                double? limitMeters = null;
                lock (_lock)
                {
                    if (!_nodeOccupancyByMap.TryGetValue(mapId, out var nodeOcc))
                    {
                        nodeOcc = new Dictionary<int, string>();
                        _nodeOccupancyByMap[mapId] = nodeOcc;
                    }
                    if (!_edgeOccupancyByMap.TryGetValue(mapId, out var edgeOcc))
                    {
                        edgeOcc = new Dictionary<(int from, int to), string>();
                        _edgeOccupancyByMap[mapId] = edgeOcc;
                    }
                    if (_robotLast.TryGetValue(ip!, out var last))
                    {
                        if (last.nodeId.HasValue) nodeOcc.Remove(last.nodeId.Value);
                        if (last.edge.HasValue) edgeOcc.Remove(last.edge.Value);
                    }
                    {
                        var forward = (from: startNodeId.Value, to: endNodeId.Value);
                        allowed = true;
                        limitMeters = allowed ? Math.Min(segLen, aheadLimit) : 0;
                        if (allowed && (limitMeters ?? 0) <= 0) allowed = false;
                        if (allowed)
                        {
                            edgeOcc[forward] = ip!;
                            _robotLast[ip!] = (endNodeId.Value, forward);
                        }
                    }
                }
                await SendAllowAsync(ip!, robotId, allowed, limitMeters, stoppingToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Traffic control error");
        }
    }

    private async Task<int?> NearestNodeIdAsync(AppDbContext db, int mapId, double x, double y, CancellationToken ct)
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

    private async Task SendAllowAsync(string ip, int robotId, bool allow, double? limitMeters, CancellationToken ct)
    {
        var payload = new
        {
            command = NatsTopics.CommandTrafficControl,
            ip,
            allow,
            limitMeters
        };
        await _nats.PublishCoreAsync($"{NatsTopics.ControlPrefix}.{robotId}", payload, ct);
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

    private class RobotSnap
    {
        public string Ip { get; set; } = "";
        public double X { get; set; }
        public double Y { get; set; }
        public string? State { get; set; }
        public int? MapId { get; set; }
        public DateTime Last { get; set; }
    }
}

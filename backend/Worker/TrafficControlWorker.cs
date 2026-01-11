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

    private readonly Dictionary<int, Dictionary<int, Occupancy>> _nodeOccupancyByMap = new();
    private readonly Dictionary<int, Dictionary<(int from, int to), Occupancy>> _edgeOccupancyByMap = new();
    private readonly Dictionary<string, (int? nodeId, (int from, int to)? edge)> _robotLast = new();
    private readonly ConcurrentDictionary<string, RobotSnap> _latest = new();
    private readonly ConcurrentDictionary<string, DateTime> _lastReroute = new();

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
                    await SendAllowAsync(ip!, robotId, true, segLen, 1.5, stoppingToken, "moving");
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
                var wrongWay = false;
                if (line != null && path?.TwoWay == false)
                {
                    var lirCheck = new LengthIndexedLine(line);
                    var startIdxC = lirCheck.Project(new Coordinate(firstPt.x, firstPt.y));
                    var endIdxC = lirCheck.Project(new Coordinate(lastPt.x, lastPt.y));
                    wrongWay = endIdxC < startIdxC;
                }
                var aheadLimit = ComputeAheadLimit(line, ip!, segLen, firstPt.x, firstPt.y, lastPt.x, lastPt.y, robotEntity, mapId);
                bool allowed;
                double? limitMeters = null;
                double speedLimit = 0;
                var deg = await db.Paths.AsNoTracking().CountAsync(p => p.MapId == mapId && (p.StartNodeId == endNodeId || p.EndNodeId == endNodeId), stoppingToken);
                if (wrongWay)
                {
                    allowed = false;
                    limitMeters = 0;
                    speedLimit = 0;
                }
                else
                {
                    (allowed, limitMeters, speedLimit) = DecideAndReserve(mapId, startNodeId!.Value, endNodeId!.Value, ip!, segLen, aheadLimit, deg, path?.TwoWay == true);
                }
                if (path?.TwoWay == true && IsStopped(ip!) && ShouldReroute(ip!))
                {
                    var rx = _latest.TryGetValue(ip!, out var snapR) ? snapR.X : (robotEntity?.Location != null ? robotEntity.Location.X : (robotEntity?.X ?? lastPt.x));
                    var ry = _latest.TryGetValue(ip!, out var snapR2) ? snapR2.Y : (robotEntity?.Location != null ? robotEntity.Location.Y : (robotEntity?.Y ?? lastPt.y));
                    var queue = scope.ServiceProvider.GetRequiredService<IRoutePlanQueue>();
                    await queue.EnqueueAsync(new RoutePlanTask { Ip = ip!, MapId = mapId, X = rx, Y = ry }, stoppingToken);
                    _lastReroute[ip!] = DateTime.UtcNow;
                }
                var newState = !allowed ? "stop" : (endDist <= 0.5 ? "idle" : "moving");
                await SendAllowAsync(ip!, robotId, allowed, limitMeters, speedLimit, stoppingToken, newState);
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

    private async Task SendAllowAsync(string ip, int robotId, bool allow, double? limitMeters, double speedLimit, CancellationToken ct, string state)
    {
        var payload = new
        {
            command = NatsTopics.CommandTrafficControl,
            ip,
            allow,
            limitMeters,
            speedLimit,
            state
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

    private class Occupancy
    {
        public string Ip { get; set; } = "";
        public double Priority { get; set; }
        public DateTime Updated { get; set; }
    }

    private double ComputePriority(string ip)
    {
        if (_latest.TryGetValue(ip, out var snap))
        {
            var baseScore = 1.0;
            var st = (snap.State ?? "").ToLowerInvariant();
            if (st.Contains("navigate") || st.Contains("move")) baseScore += 1.0;
            if (st.Contains("idle") || st.Contains("stop")) baseScore -= 0.5;
            var tie = Math.Abs(ip.GetHashCode() % 1000) / 1000.0;
            return baseScore + tie;
        }
        return 1.0;
    }

    private double ComputeAheadLimit(LineString? line, string ip, double segLen, double sx, double sy, double ex, double ey, dynamic? robotEntity, int mapId)
    {
        var limit = segLen;
        if (line == null) return limit;
        var lir = new LengthIndexedLine(line);
        double rx0 = sx;
        double ry0 = sy;
        if (_latest.TryGetValue(ip, out var mySnap))
        {
            rx0 = mySnap.X;
            ry0 = mySnap.Y;
        }
        else
        {
            rx0 = robotEntity?.Location != null ? robotEntity.Location.X : (robotEntity?.X ?? sx);
            ry0 = robotEntity?.Location != null ? robotEntity.Location.Y : (robotEntity?.Y ?? sy);
        }
        var startIdx = lir.Project(new Coordinate(rx0, ry0));
        var endIdx = lir.Project(new Coordinate(ex, ey));
        var forwardDir = endIdx >= startIdx;
        var near = _latest.Values
            .Where(r => r.MapId == mapId && r.Ip != ip && (DateTime.UtcNow - r.Last) <= TimeSpan.FromSeconds(5))
            .Select(r =>
            {
                var ridx = lir.Project(new Coordinate(r.X, r.Y));
                var proj = lir.ExtractPoint(ridx);
                var dx = r.X - proj.X;
                var dy = r.Y - proj.Y;
                var perpDist = Math.Sqrt(dx * dx + dy * dy);
                var st = (r.State ?? "").ToLowerInvariant();
                var stopped = st.Contains("stop") || st.Contains("idle");
                var buffer = stopped ? 1.2 : 2.5;
                return (ridx, ip2: r.Ip, perpDist, buffer);
            })
            .Where(t => t.perpDist <= 0.8 && ((forwardDir && t.ridx > startIdx && t.ridx <= endIdx) || (!forwardDir && t.ridx < startIdx && t.ridx >= endIdx)))
            .OrderBy(t => Math.Abs(t.ridx - startIdx))
            .FirstOrDefault();
        if (near.ip2 != null)
        {
            var delta = forwardDir ? (near.ridx - startIdx) : (startIdx - near.ridx);
            if (delta <= near.buffer) limit = 0.0;
            else limit = Math.Max(0, Math.Min(segLen, delta - near.buffer));
        }
        return limit;
    }

    private (bool allowed, double? limitMeters, double speed) DecideAndReserve(int mapId, int startNodeId, int endNodeId, string ip, double segLen, double aheadLimit, int deg, bool isTwoWay)
    {
        bool allowed = true;
        double? limitMeters = null;
        double speedLimit = 0;
        lock (_lock)
        {
            if (!_nodeOccupancyByMap.TryGetValue(mapId, out var nodeOcc))
            {
                nodeOcc = new Dictionary<int, Occupancy>();
                _nodeOccupancyByMap[mapId] = nodeOcc;
            }
            if (!_edgeOccupancyByMap.TryGetValue(mapId, out var edgeOcc))
            {
                edgeOcc = new Dictionary<(int from, int to), Occupancy>();
                _edgeOccupancyByMap[mapId] = edgeOcc;
            }
            var now = DateTime.UtcNow;
            var leaseMs = 20000;
            if (nodeOcc.Count > 0)
            {
                var expiredNodes = nodeOcc.Where(kv => (now - kv.Value.Updated).TotalMilliseconds > leaseMs).Select(kv => kv.Key).ToList();
                foreach (var nid in expiredNodes) nodeOcc.Remove(nid);
            }
            if (edgeOcc.Count > 0)
            {
                var expiredEdges = edgeOcc.Where(kv => (now - kv.Value.Updated).TotalMilliseconds > leaseMs).Select(kv => kv.Key).ToList();
                foreach (var eid in expiredEdges) edgeOcc.Remove(eid);
            }
            if (_robotLast.TryGetValue(ip, out var last))
            {
                if (last.nodeId.HasValue)
                {
                    if (nodeOcc.TryGetValue(last.nodeId.Value, out var occN) && occN.Ip == ip) nodeOcc.Remove(last.nodeId.Value);
                }
                if (last.edge.HasValue)
                {
                    if (edgeOcc.TryGetValue(last.edge.Value, out var occE) && occE.Ip == ip) edgeOcc.Remove(last.edge.Value);
                }
            }
            var forward = (from: startNodeId, to: endNodeId);
            var reverse = (from: endNodeId, to: startNodeId);
            var myPriority = ComputePriority(ip);
            if (isTwoWay || deg > 2)
            {
                if (nodeOcc.TryGetValue(endNodeId, out var nocc) && nocc.Ip != ip) allowed = false;
            }
            if (allowed)
            {
                if (edgeOcc.TryGetValue(forward, out var eocc) && eocc.Ip != ip) allowed = false;
            }
            if (allowed)
            {
                if (edgeOcc.TryGetValue(reverse, out var reocc) && reocc.Ip != ip) allowed = false;
            }
            limitMeters = allowed ? Math.Min(segLen, aheadLimit) : 0;
            if ((limitMeters ?? 0) <= 0) allowed = false;
            if (allowed)
            {
                var occ = new Occupancy { Ip = ip, Priority = myPriority, Updated = now };
                edgeOcc[forward] = occ;
                if (isTwoWay || deg > 2) nodeOcc[endNodeId] = occ;
                _robotLast[ip] = (endNodeId, forward);
                var ratio = Math.Max(0, Math.Min(1, (limitMeters ?? 0) / Math.Max(0.1, segLen)));
                speedLimit = Math.Max(0.1, Math.Min(2.0, 0.5 + 1.5 * ratio));
            }
        }
        return (allowed, limitMeters, speedLimit);
    }

    private bool IsStopped(string ip)
    {
        if (_latest.TryGetValue(ip, out var snap))
        {
            var st = (snap.State ?? "").ToLowerInvariant();
            return st.Contains("stop");
        }
        return false;
    }

    private bool ShouldReroute(string ip)
    {
        var now = DateTime.UtcNow;
        if (_lastReroute.TryGetValue(ip, out var last))
        {
            if ((now - last) < TimeSpan.FromSeconds(5)) return false;
        }
        return true;
    }
}

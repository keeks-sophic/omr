using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using Robot.Topics;

namespace Robot.Services;

public class CommandListenerService
{
    private readonly ILogger<CommandListenerService> _logger;
    private readonly NatsService _nats;
    private readonly TelemetryService _telemetry;
    private readonly List<NATS.Client.IAsyncSubscription> _subs = new();

    public CommandListenerService(ILogger<CommandListenerService> logger, NatsService nats, TelemetryService telemetry)
    {
        _logger = logger;
        _nats = nats;
        _telemetry = telemetry;
    }

    public Task StartAsync(string name, string ip, IEnumerable<string> subjects, CancellationToken token)
    {
        if (_subs.Count > 0) return Task.CompletedTask;
        try
        {
            foreach (var subject in subjects)
            {
                var sub = _nats.Subscribe(subject, (s, e) =>
                {
                    var data = e.Message.Data;
                    var text = data != null ? Encoding.UTF8.GetString(data) : "";
                    try
                    {
                        _logger.LogInformation("Received command on {Subject}: {Payload}", subject, text);
                        var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(text) ? "{}" : text);
                        var targetIp = doc.RootElement.TryGetProperty("ip", out var ipProp)
                            ? ipProp.GetString()
                            : (doc.RootElement.TryGetProperty("Ip", out var IpProp) ? IpProp.GetString() : null);
                        var command = doc.RootElement.TryGetProperty("command", out var cmdProp)
                            ? cmdProp.GetString()
                            : (doc.RootElement.TryGetProperty("Command", out var CmdProp) ? CmdProp.GetString() : null);
                        if (!string.IsNullOrWhiteSpace(targetIp) && string.Equals(targetIp, ip, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(command))
                        {
                            if (string.Equals(command, "route.assign", StringComparison.OrdinalIgnoreCase))
                            {
                                if (doc.RootElement.TryGetProperty("route", out var routeEl) &&
                                    routeEl.TryGetProperty("nodeIds", out var nodesEl) && nodesEl.ValueKind == JsonValueKind.Array)
                                {
                                    var nodes = nodesEl.EnumerateArray().Select(x => x.GetInt32()).ToArray();
                                    _telemetry.SetRoute(nodes);
                                    _logger.LogInformation("Route assigned with {Count} nodes", nodes.Length);
                                }
                                if (doc.RootElement.TryGetProperty("route", out var route2) &&
                                    route2.TryGetProperty("mapId", out var mapEl) &&
                                    mapEl.ValueKind == JsonValueKind.Number)
                                {
                                    var mapId = mapEl.GetInt32();
                                    _telemetry.SetMap(mapId);
                                    _logger.LogInformation("Map assigned: {MapId}", mapId);
                                }
                                var hasNodes = doc.RootElement.TryGetProperty("nodes", out var coordsEl) && coordsEl.ValueKind == JsonValueKind.Array;
                                var speed = doc.RootElement.TryGetProperty("speed", out var spEl) && spEl.ValueKind == JsonValueKind.Number ? spEl.GetDouble() : (double?)null;
                                if (hasNodes)
                                {
                                    var pts = coordsEl.EnumerateArray()
                                        .Select(el =>
                                        {
                                            var x = el.TryGetProperty("x", out var xx) && xx.ValueKind == JsonValueKind.Number ? xx.GetDouble() : 0;
                                            var y = el.TryGetProperty("y", out var yy) && yy.ValueKind == JsonValueKind.Number ? yy.GetDouble() : 0;
                                            return (x, y);
                                        })
                                        .ToList();
                                    int? mapId2 = null;
                                    if (doc.RootElement.TryGetProperty("route", out var r3) && r3.TryGetProperty("mapId", out var mid2) && mid2.ValueKind == JsonValueKind.Number)
                                        mapId2 = mid2.GetInt32();
                                    _telemetry.SetRoutePath(pts, speed, mapId2);
                                    _logger.LogInformation("Route path with {Count} points applied; speed={Speed} m/s", pts.Count, speed ?? 0.1);
                                }
                            }
                            else if (string.Equals(command, "route.plan", StringComparison.OrdinalIgnoreCase))
                            {
                                if (doc.RootElement.TryGetProperty("route", out var routeEl))
                                {
                                    int? mapId = null;
                                    if (routeEl.TryGetProperty("mapId", out var mid) && mid.ValueKind == JsonValueKind.Number)
                                        mapId = mid.GetInt32();
                                    var hasNodes = routeEl.TryGetProperty("nodes", out var nodesEl) && nodesEl.ValueKind == JsonValueKind.Array;
                                    var speed = routeEl.TryGetProperty("speed", out var spEl) && spEl.ValueKind == JsonValueKind.Number ? spEl.GetDouble() : (double?)null;
                                    if (routeEl.TryGetProperty("nodeIds", out var nodeIdsEl) && nodeIdsEl.ValueKind == JsonValueKind.Array)
                                    {
                                        var nodeIds = nodeIdsEl.EnumerateArray().Select(x => x.GetInt32()).ToArray();
                                        _telemetry.SetRoute(nodeIds);
                                    }
                                    if (hasNodes)
                                    {
                                        var pts = nodesEl.EnumerateArray()
                                            .Select(el =>
                                            {
                                                var x = el.TryGetProperty("x", out var xx) && xx.ValueKind == JsonValueKind.Number ? xx.GetDouble() : 0;
                                                var y = el.TryGetProperty("y", out var yy) && yy.ValueKind == JsonValueKind.Number ? yy.GetDouble() : 0;
                                                return (x, y);
                                            })
                                            .ToList();
                                        _telemetry.SetRoutePath(pts, speed, mapId);
                                        _logger.LogInformation("Route plan applied with {Count} nodes", pts.Count);
                                    }
                                }
                            }
                            else if (string.Equals(command, "traffic.control", StringComparison.OrdinalIgnoreCase))
                            {
                                var allow = doc.RootElement.TryGetProperty("allow", out var al) && al.ValueKind == JsonValueKind.True
                                    || (doc.RootElement.TryGetProperty("allow", out var al2) && al2.ValueKind == JsonValueKind.Number && al2.GetInt32() != 0);
                                double? limitMeters = null;
                                if (doc.RootElement.TryGetProperty("limitMeters", out var lm) && lm.ValueKind == JsonValueKind.Number)
                                    limitMeters = lm.GetDouble();
                                double? speedLimit = null;
                                if (doc.RootElement.TryGetProperty("speedLimit", out var sl) && sl.ValueKind == JsonValueKind.Number)
                                    speedLimit = sl.GetDouble();
                                _telemetry.SetTrafficControl(allow, limitMeters, speedLimit);
                                _logger.LogInformation("Traffic control: allow={Allow}, limitMeters={Limit}, speedLimit={Speed}", allow, limitMeters, speedLimit);
                            }
                            else if (string.Equals(command, "robot.sync", StringComparison.OrdinalIgnoreCase))
                            {
                                if (doc.RootElement.TryGetProperty("robot", out var rob) && rob.ValueKind == JsonValueKind.Object)
                                {
                                    var name2 = rob.TryGetProperty("name", out var nn) ? nn.GetString() : (rob.TryGetProperty("Name", out var nn2) ? nn2.GetString() : null);
                                    if (rob.TryGetProperty("id", out var idEl) && idEl.ValueKind == JsonValueKind.Number)
                                    {
                                        var rid = idEl.GetInt32();
                                        _telemetry.SetRobotId(rid);
                                        EnsureIdSubscriptions(rid);
                                    }
                                    int? mid2 = null;
                                    if (rob.TryGetProperty("mapId", out var midEl) && midEl.ValueKind == JsonValueKind.Number) mid2 = midEl.GetInt32();
                                    double? x2 = null, y2 = null, batt2 = null;
                                    if (rob.TryGetProperty("x", out var xx) && xx.ValueKind == JsonValueKind.Number) x2 = xx.GetDouble();
                                    else if (rob.TryGetProperty("X", out var xX) && xX.ValueKind == JsonValueKind.Number) x2 = xX.GetDouble();
                                    if (rob.TryGetProperty("y", out var yy) && yy.ValueKind == JsonValueKind.Number) y2 = yy.GetDouble();
                                    else if (rob.TryGetProperty("Y", out var yY) && yY.ValueKind == JsonValueKind.Number) y2 = yY.GetDouble();
                                    if (rob.TryGetProperty("battery", out var bb) && bb.ValueKind == JsonValueKind.Number) batt2 = bb.GetDouble();
                                    else if (rob.TryGetProperty("Battery", out var bB) && bB.ValueKind == JsonValueKind.Number) batt2 = bB.GetDouble();
                                    var state2 = rob.TryGetProperty("state", out var ss) ? ss.GetString() : (rob.TryGetProperty("State", out var sS) ? sS.GetString() : null);
                                    _telemetry.ApplySnapshot(name2, mid2, x2, y2, batt2, state2);
                                    if (mid2.HasValue) _telemetry.SetMap(mid2.Value);
                                    _logger.LogInformation("Applied sync snapshot from backend");
                                }
                            }
                            else
                            {
                                _telemetry.ApplyCommand(command!);
                                _telemetry.PublishStatusAsync().ConfigureAwait(false);
                                _logger.LogInformation("Applied command: {Command} for {Ip}", command, ip);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse command payload: {Payload}", text);
                    }
                });
                _subs.Add(sub);
            }
        }
        catch
        {
        }
        _logger.LogInformation("Command listener started. Name: {Name}", name);
        return Task.CompletedTask;
    }

    private void EnsureIdSubscriptions(int id)
    {
        var subjects = new[]
        {
            $"{NatsSubjects.CommandPrefix}.{id}.>",
            $"{NatsSubjects.RoutePlanPrefix}.{id}",
            $"{NatsSubjects.ControlPrefix}.{id}",
            $"{NatsSubjects.SyncPrefix}.{id}"
        };
        foreach (var subject in subjects)
        {
            var sub = _nats.Subscribe(subject, (s, e) =>
            {
                var data = e.Message.Data;
                var text = data != null ? Encoding.UTF8.GetString(data) : "";
                try
                {
                    _logger.LogInformation("Received command on {Subject}: {Payload}", subject, text);
                    var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(text) ? "{}" : text);
                    var command = doc.RootElement.TryGetProperty("command", out var cmdProp) ? cmdProp.GetString() : null;
                    if (!string.IsNullOrWhiteSpace(command))
                    {
                        if (string.Equals(command, "traffic.control", StringComparison.OrdinalIgnoreCase))
                        {
                            var allow = doc.RootElement.TryGetProperty("allow", out var al) && al.ValueKind == JsonValueKind.True
                                || (doc.RootElement.TryGetProperty("allow", out var al2) && al2.ValueKind == JsonValueKind.Number && al2.GetInt32() != 0);
                            double? limitMeters = null;
                            if (doc.RootElement.TryGetProperty("limitMeters", out var lm) && lm.ValueKind == JsonValueKind.Number)
                                limitMeters = lm.GetDouble();
                            double? speedLimit = null;
                            if (doc.RootElement.TryGetProperty("speedLimit", out var sl) && sl.ValueKind == JsonValueKind.Number)
                                speedLimit = sl.GetDouble();
                            _telemetry.SetTrafficControl(allow, limitMeters, speedLimit);
                        }
                        else if (string.Equals(command, "route.plan", StringComparison.OrdinalIgnoreCase))
                        {
                            if (doc.RootElement.TryGetProperty("route", out var routeEl))
                            {
                                int? mapId = null;
                                if (routeEl.TryGetProperty("mapId", out var mid) && mid.ValueKind == JsonValueKind.Number)
                                    mapId = mid.GetInt32();
                                var hasNodes = routeEl.TryGetProperty("nodes", out var nodesEl) && nodesEl.ValueKind == JsonValueKind.Array;
                                var speed = routeEl.TryGetProperty("speed", out var spEl) && spEl.ValueKind == JsonValueKind.Number ? spEl.GetDouble() : (double?)null;
                                if (hasNodes)
                                {
                                    var pts = nodesEl.EnumerateArray()
                                        .Select(el =>
                                        {
                                            var x = el.TryGetProperty("x", out var xx) && xx.ValueKind == JsonValueKind.Number ? xx.GetDouble() : 0;
                                            var y = el.TryGetProperty("y", out var yy) && yy.ValueKind == JsonValueKind.Number ? yy.GetDouble() : 0;
                                            return (x, y);
                                        })
                                        .ToList();
                                    _telemetry.SetRoutePath(pts, speed, mapId);
                                }
                            }
                        }
                        else if (string.Equals(command, "robot.sync", StringComparison.OrdinalIgnoreCase))
                        {
                            if (doc.RootElement.TryGetProperty("robot", out var rob) && rob.ValueKind == JsonValueKind.Object)
                            {
                                int? mid2 = null;
                                if (rob.TryGetProperty("mapId", out var midEl) && midEl.ValueKind == JsonValueKind.Number) mid2 = midEl.GetInt32();
                                _telemetry.SetMap(mid2);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse command payload: {Payload}", text);
                }
            });
            _subs.Add(sub);
        }
    }
}

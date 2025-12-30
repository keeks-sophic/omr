using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using Backend.Infrastructure.Persistence;
using Backend.Model;
using Backend.Topics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.SignalR;
using Backend.SignalR;
using Backend.Data;
using Microsoft.Extensions.DependencyInjection;
using Backend.Service;
using Backend.Topics;

namespace Backend.Worker;

public class RobotTelemetrySubscriber : BackgroundService
{
    private readonly ILogger<RobotTelemetrySubscriber> _logger;
    private readonly IServiceProvider _sp;
    private readonly NatsService _nats;
    private readonly IOptions<NatsOptions> _options;
    private readonly ConcurrentDictionary<string, TelemetrySnap> _lastTelemetry = new();
    private readonly IHubContext<RobotsHub> _hub;

    public RobotTelemetrySubscriber(ILogger<RobotTelemetrySubscriber> logger, IServiceProvider sp, NatsService nats, IOptions<NatsOptions> options, IHubContext<RobotsHub> hub)
    {
        _logger = logger;
        _sp = sp;
        _nats = nats;
        _options = options;
        _hub = hub;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _nats.ConnectAsync(_options.Value.Url, stoppingToken);
        await _nats.EnsureStreamAsync(_options.Value.TelemetryWildcardStream ?? "ROBOTS_TELEMETRY", $"{NatsTopics.TelemetryPrefix}.>");
        await _nats.EnsureStreamAsync(_options.Value.CommandWildcardStream ?? "ROBOTS_COMMAND", $"{NatsTopics.CommandPrefix}.>");
        await _nats.EnsureStreamAsync(_options.Value.RouteWildcardStream ?? "ROBOTS_ROUTE", $"{NatsTopics.RoutePlanPrefix}.>", $"{NatsTopics.RouteSegmentPrefix}.>");
        await _nats.EnsureStreamAsync(_options.Value.ControlWildcardStream ?? "ROBOTS_CONTROL", $"{NatsTopics.ControlPrefix}.>", $"{NatsTopics.SyncPrefix}.>");
        _nats.Subscribe($"{NatsTopics.TelemetryPrefix}.>", async (s, e) =>
        {
            try
            {
                var payload = e.Message.Data != null ? Encoding.UTF8.GetString(e.Message.Data) : "{}";
                var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(payload) ? "{}" : payload);
                var ip = doc.RootElement.TryGetProperty("Ip", out var ipEl) ? ipEl.GetString() : (doc.RootElement.TryGetProperty("ip", out var ip2) ? ip2.GetString() : null);
                if (string.IsNullOrWhiteSpace(ip)) return;
                var name = doc.RootElement.TryGetProperty("Name", out var nEl) ? nEl.GetString() : (doc.RootElement.TryGetProperty("name", out var n2) ? n2.GetString() : null);
                double? x = doc.RootElement.TryGetProperty("X", out var xEl) && xEl.ValueKind == JsonValueKind.Number ? xEl.GetDouble() : (doc.RootElement.TryGetProperty("x", out var x2) && x2.ValueKind == JsonValueKind.Number ? x2.GetDouble() : (double?)null);
                double? y = doc.RootElement.TryGetProperty("Y", out var yEl) && yEl.ValueKind == JsonValueKind.Number ? yEl.GetDouble() : (doc.RootElement.TryGetProperty("y", out var y2) && y2.ValueKind == JsonValueKind.Number ? y2.GetDouble() : (double?)null);
                var battery = doc.RootElement.TryGetProperty("Battery", out var bEl) && bEl.ValueKind == JsonValueKind.Number ? bEl.GetDouble() : (doc.RootElement.TryGetProperty("battery", out var b2) && b2.ValueKind == JsonValueKind.Number ? b2.GetDouble() : 0);
                var state = doc.RootElement.TryGetProperty("State", out var sEl) ? sEl.GetString() : (doc.RootElement.TryGetProperty("state", out var s2) ? s2.GetString() : null);
                var mapId = doc.RootElement.TryGetProperty("MapId", out var mEl) && mEl.ValueKind == JsonValueKind.Number ? (int?)mEl.GetInt32() : (doc.RootElement.TryGetProperty("mapId", out var m2) && m2.ValueKind == JsonValueKind.Number ? (int?)m2.GetInt32() : null);
                var now = DateTime.UtcNow;
                var existed = _lastTelemetry.TryGetValue(ip!, out var prevSnap);
                using var scope = _sp.CreateScope();
                var robots = scope.ServiceProvider.GetRequiredService<RobotRepository>();
                if (!existed)
                {
                    var robDb = await robots.GetRobotByIpAsync(ip!, stoppingToken);
                    if (robDb == null)
                    {
                        var created = await robots.UpsertRobotTelemetryAsync(ip!, name, x, y, battery, state, mapId, stoppingToken);
                        var replyCreate = new
                        {
                            command = NatsTopics.CommandRobotSync,
                            ip = ip,
                            robot = new
                            {
                                id = created.Id,
                                name = created.Name,
                                mapId = created.MapId,
                                x = created.X,
                                y = created.Y,
                                battery = created.Battery,
                                state = created.State
                            }
                        };
                        await _nats.PublishAsync($"{NatsTopics.SyncPrefix}.{created.Id}", replyCreate, stoppingToken);
                        await _nats.PublishAsync($"{NatsTopics.SyncPrefix}.{ip}", replyCreate, stoppingToken);
                    }
                    else
                    {
                        var replyDb = new
                        {
                            command = NatsTopics.CommandRobotSync,
                            ip = ip,
                            robot = new
                            {
                                id = robDb.Id,
                                name = robDb.Name,
                                mapId = robDb.MapId,
                                x = robDb.X,
                                y = robDb.Y,
                                battery = robDb.Battery,
                                state = robDb.State
                            }
                        };
                        await _nats.PublishAsync($"{NatsTopics.SyncPrefix}.{robDb.Id}", replyDb, stoppingToken);
                        await _nats.PublishAsync($"{NatsTopics.SyncPrefix}.{ip}", replyDb, stoppingToken);
                    }
                }
                var shouldUpsert = !existed
                    || (state != null && (prevSnap?.State == null || !string.Equals(prevSnap.State, state, StringComparison.OrdinalIgnoreCase)))
                    || (x.HasValue && (!prevSnap?.X.HasValue ?? true || Math.Abs((prevSnap?.X ?? 0) - x.Value) > 1e-6))
                    || (y.HasValue && (!prevSnap?.Y.HasValue ?? true || Math.Abs((prevSnap?.Y ?? 0) - y.Value) > 1e-6))
                    || (mapId.HasValue && (prevSnap?.MapId != mapId));
                if (shouldUpsert)
                {
                    await robots.UpsertRobotTelemetryAsync(ip!, name, x, y, battery, state, mapId, stoppingToken);
                }
                var snap = new TelemetrySnap
                {
                    Name = name ?? prevSnap?.Name ?? ip!,
                    Ip = ip!,
                    X = x ?? prevSnap?.X,
                    Y = y ?? prevSnap?.Y,
                    Battery = battery,
                    State = state ?? prevSnap?.State,
                    MapId = mapId ?? prevSnap?.MapId,
                    LastActive = now
                };
                _lastTelemetry[ip!] = snap;
                var resolvedName = snap.Name;
                var resolvedMapId = snap.MapId;
                var resolvedX = snap.X ?? 0;
                var resolvedY = snap.Y ?? 0;
                var resolvedBattery = snap.Battery;
                var resolvedState = snap.State ?? "idle";
                var resolvedConnected = true;
                var resolvedLastActive = snap.LastActive;
                await _hub.Clients.All.SendAsync(SignalRTopics.Identity, new { name = resolvedName, ip = ip }, stoppingToken);
                var telemetryPayload = new
                {
                    name = resolvedName,
                    ip = ip,
                    x = resolvedX,
                    y = resolvedY,
                    state = resolvedState,
                    battery = resolvedBattery,
                    connected = resolvedConnected,
                    lastActive = resolvedLastActive,
                    mapId = resolvedMapId
                };
                await _hub.Clients.All.SendAsync(SignalRTopics.Telemetry, telemetryPayload, stoppingToken);
                if (resolvedMapId.HasValue) await _hub.Clients.Group($"map:{resolvedMapId.Value}").SendAsync(SignalRTopics.Telemetry, telemetryPayload, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to process telemetry");
            }
        });
        using (var scope = _sp.CreateScope())
        {
            var robots = scope.ServiceProvider.GetRequiredService<RobotRepository>();
            var stale = await robots.MarkStaleRobotsDisconnectedAsync(TimeSpan.FromSeconds(3), stoppingToken);
            foreach (var rob in stale)
            {
                var payload = new
                {
                    name = rob.Name,
                    ip = rob.Ip,
                    x = rob.X,
                    y = rob.Y,
                    state = rob.State,
                    battery = rob.Battery,
                    connected = rob.Connected,
                    lastActive = rob.LastActive,
                    mapId = rob.MapId
                };
                await _hub.Clients.All.SendAsync(SignalRTopics.Telemetry, payload, stoppingToken);
                if (rob.MapId.HasValue)
                {
                    await _hub.Clients.Group($"map:{rob.MapId.Value}").SendAsync(SignalRTopics.Telemetry, payload, stoppingToken);
                }
            }
        }
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.UtcNow;
                foreach (var kv in _lastTelemetry.ToArray())
                {
                    var ip = kv.Key;
                    var snap = kv.Value;
                    if (now - snap.LastActive > TimeSpan.FromSeconds(3))
                    {
                        using var scope = _sp.CreateScope();
                        var robots = scope.ServiceProvider.GetRequiredService<RobotRepository>();
                        await robots.UpsertRobotTelemetryAsync(ip, snap.Name, snap.X, snap.Y, snap.Battery, snap.State, snap.MapId, stoppingToken);
                        await robots.MarkRobotDisconnectedAsync(ip, stoppingToken);
                        var payload = new
                        {
                            name = snap.Name,
                            ip = ip,
                            x = snap.X ?? 0,
                            y = snap.Y ?? 0,
                            state = snap.State ?? "idle",
                            battery = snap.Battery,
                            connected = false,
                            lastActive = DateTime.UtcNow,
                            mapId = snap.MapId
                        };
                        await _hub.Clients.All.SendAsync(SignalRTopics.Telemetry, payload, stoppingToken);
                        if (snap.MapId.HasValue)
                        {
                            await _hub.Clients.Group($"map:{snap.MapId.Value}").SendAsync(SignalRTopics.Telemetry, payload, stoppingToken);
                        }
                        _lastTelemetry.TryRemove(ip, out _);
                    }
                }
            }
            catch { }
            await Task.Delay(1000, stoppingToken);
        }
    }

    private class TelemetrySnap
    {
        public string Name { get; set; } = "";
        public string Ip { get; set; } = "";
        public double? X { get; set; }
        public double? Y { get; set; }
        public double Battery { get; set; }
        public string? State { get; set; }
        public int? MapId { get; set; }
        public DateTime LastActive { get; set; }
    }
}

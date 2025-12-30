using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using Backend.Infrastructure.Persistence;
using Backend.Model;
using Backend.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.SignalR;
using Backend.Hubs;
using Backend.Database;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Services;

public class RobotTelemetrySubscriber : BackgroundService
{
    private readonly ILogger<RobotTelemetrySubscriber> _logger;
    private readonly IServiceProvider _sp;
    private readonly NatsService _nats;
    private readonly IOptions<NatsOptions> _options;
    private readonly ConcurrentDictionary<string, DateTime> _lastSeen = new();
    private readonly ConcurrentDictionary<string, string> _lastState = new();
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
        await _nats.EnsureStreamAsync(_options.Value.TelemetryStream ?? "ROBOT_TELEMETRY", _options.Value.TelemetrySubject);
        await _nats.EnsureStreamAsync(_options.Value.CommandStream ?? "ROBOT_COMMAND", _options.Value.CommandSubject);
        _nats.Subscribe(_options.Value.TelemetrySubject, async (s, e) =>
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
                _lastSeen[ip!] = DateTime.UtcNow;
                using var scope = _sp.CreateScope();
                var robots = scope.ServiceProvider.GetRequiredService<RobotRepository>();
                var rob = await robots.UpsertRobotTelemetryAsync(ip!, name, x, y, battery, state, mapId, stoppingToken);
                _lastState[ip!] = rob.State;
                await _hub.Clients.All.SendAsync("identity", new { name = rob.Name, ip = rob.Ip }, stoppingToken);
                var telemetryPayload = new
                {
                    name = rob.Name,
                    ip = rob.Ip,
                    x = rob.X,
                    y = rob.Y,
                    state = rob.State,
                    battery = rob.Battery,
                    connected = rob.Connected,
                    lastActive = rob.LastActive
                };
                await _hub.Clients.All.SendAsync("telemetry", telemetryPayload, stoppingToken);
                if (rob.MapId.HasValue)
                {
                    await _hub.Clients.Group($"map:{rob.MapId.Value}").SendAsync("telemetry", telemetryPayload, stoppingToken);
                }
                var reply = new
                {
                    command = "robot.sync",
                    ip = ip,
                    robot = new
                    {
                        mapId = rob.MapId,
                        x = rob.X,
                        y = rob.Y,
                        location = rob.Location != null ? new { x = rob.Location.X, y = rob.Location.Y } : null
                    }
                };
                await _nats.PublishAsync(_options.Value.CommandSubject, reply, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to process telemetry");
            }
        });
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.UtcNow;
                foreach (var kv in _lastSeen.ToArray())
                {
                    if (now - kv.Value > TimeSpan.FromSeconds(3))
                    {
                        using var scope = _sp.CreateScope();
                        var robots = scope.ServiceProvider.GetRequiredService<RobotRepository>();
                        await robots.MarkRobotDisconnectedAsync(kv.Key, stoppingToken);
                        var db2 = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                        var rob = await db2.Robots.FirstOrDefaultAsync(r => r.Ip == kv.Key, stoppingToken);
                        if (rob != null)
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
                                lastActive = rob.LastActive
                            };
                            await _hub.Clients.All.SendAsync("telemetry", payload, stoppingToken);
                            if (rob.MapId.HasValue)
                            {
                                await _hub.Clients.Group($"map:{rob.MapId.Value}").SendAsync("telemetry", payload, stoppingToken);
                            }
                        }
                    }
                }
            }
            catch { }
            await Task.Delay(1000, stoppingToken);
        }
    }
}

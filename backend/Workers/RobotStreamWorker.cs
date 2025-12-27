using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using backend.Options;
using backend.Services;
using Microsoft.AspNetCore.SignalR;
using backend.Hubs;
using backend.Mappers;
using NATS.Client;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;
using System.Collections.Concurrent;

﻿namespace backend.Workers;

public class RobotStreamWorker : BackgroundService
{
    private readonly ILogger<RobotStreamWorker> _logger;
    private readonly NatsService _nats;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<NatsOptions> _options;
    private readonly IHubContext<RobotHub> _hub;
    private IAsyncSubscription? _teleSub;
    private IAsyncSubscription? _cmdSub;
    private readonly ConcurrentDictionary<string, DateTime> _lastTelemetrySeen = new(StringComparer.OrdinalIgnoreCase);

    public RobotStreamWorker(
        ILogger<RobotStreamWorker> logger,
        NatsService nats,
        IOptions<NatsOptions> options,
        IHubContext<RobotHub> hub,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _nats = nats;
        _options = options;
        _hub = hub;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var opts = _options.Value;

        await _nats.ConnectAsync(opts.NatsUrl, stoppingToken);
        await _nats.EnsureStreamAsync(opts.TelemetryStream!, opts.TelemetrySubject);
        await _nats.EnsureStreamAsync(opts.CommandStream!, opts.CommandSubject);

      
        try
        {
            _teleSub = _nats.Subscribe(opts.TelemetrySubject, (s, e) =>
            {
                var json = e.Message.Data != null ? Encoding.UTF8.GetString(e.Message.Data) : "{}";
                try
                {
                    var doc = JsonDocument.Parse(json);
                    var name = GetString(doc.RootElement, "Name", "name");
                    var ip = GetString(doc.RootElement, "Ip", "ip");
                    var x = GetDouble(doc.RootElement, "X", "x");
                    var y = GetDouble(doc.RootElement, "Y", "y");
                    var state = GetString(doc.RootElement, "State", "state");
                    var battery = GetDouble(doc.RootElement, "Battery", "battery");
                    var mapId = doc.RootElement.TryGetProperty("MapId", out var mid) && mid.ValueKind == JsonValueKind.Number ? mid.GetInt32() : (int?)null;
                    if (!string.IsNullOrWhiteSpace(ip))
                    {
                        var first = !_lastTelemetrySeen.TryGetValue(ip!, out _);
                        _lastTelemetrySeen[ip!] = DateTime.UtcNow;
                        using var scope = _scopeFactory.CreateScope();
                        var robots = scope.ServiceProvider.GetRequiredService<IRobotService>();
                        var r = robots.UpdateTelemetry(ip!, first, name, x, y, state, battery, mapId);
                        _logger.LogInformation("Telemetry updated: {Name} Ip={Ip} x={X} y={Y} state={State} batt={Battery}", name, ip, x, y, state, battery);
                        if (r != null)
                        {
                            var dto = RobotMapper.ToDto(r);
                            _hub.Clients.All.SendAsync("telemetry", dto, stoppingToken);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse telemetry payload: {Json}", json);
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to subscribe to telemetry subject: {Subject}", opts.TelemetrySubject);
        }

        try
        {
            _cmdSub = _nats.Subscribe(opts.CommandSubject, (s, e) =>
            {
                var json = e.Message.Data != null ? Encoding.UTF8.GetString(e.Message.Data) : "{}";
                try
                {
                    var doc = JsonDocument.Parse(json);
                    var cmd = GetString(doc.RootElement, "command", "Command");
                    if (!string.Equals(cmd, "navigate.request", StringComparison.OrdinalIgnoreCase)) return;
                    var ip = GetString(doc.RootElement, "ip", "Ip");
                    var destEl = doc.RootElement.TryGetProperty("dest", out var d) ? d : default;
                    if (string.IsNullOrWhiteSpace(ip) || destEl.ValueKind != JsonValueKind.Object) return;
                    var dx = GetDouble(destEl, "x", "X");
                    var dy = GetDouble(destEl, "y", "Y");
                    var mapId = destEl.TryGetProperty("mapId", out var midEl) && midEl.ValueKind == JsonValueKind.Number ? midEl.GetInt32() : 0;
                    if (!dx.HasValue || !dy.HasValue || mapId <= 0) return;
                    var payload = new { ip = ip, command = "route.assign", route = new { robotIp = ip, mapId = mapId, nodeIds = Array.Empty<int>(), pathIds = Array.Empty<int>() }, speed = 0.1, ts = DateTime.UtcNow };
                    _nats.PublishJsonAsync(_options.Value.CommandSubject!, payload).ConfigureAwait(false);
                    _logger.LogInformation("Dispatched route.assign to {Ip}", ip);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to process command payload: {Json}", json);
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to subscribe to command subject: {Subject}", opts.CommandSubject);
        }

        // try
        // {
        //     _identSub = _nats.Subscribe(opts.CommandSubject, (s, e) =>
        //     {
        //         var json = e.Message.Data != null ? Encoding.UTF8.GetString(e.Message.Data) : "{}";
        //         try
        //         {
        //             var doc = JsonDocument.Parse(json);
        //             var name = GetString(doc.RootElement, "Name", "name");
        //             var ip = GetString(doc.RootElement, "Ip", "ip");
        //             var connectedProp = doc.RootElement.TryGetProperty("Connected", out var c) ? c.ValueKind == JsonValueKind.True : (bool?)null;
        //             var eventType = GetString(doc.RootElement, "Event", "event");
        //             var isDisconnected = (connectedProp.HasValue && connectedProp.Value == false) || string.Equals(eventType, "Disconnected", StringComparison.OrdinalIgnoreCase);
        //             if (isDisconnected)
        //             {
        //                 using var scope = _scopeFactory.CreateScope();
        //                 var robots = scope.ServiceProvider.GetRequiredService<IRobotService>();
        //                 robots.MarkDisconnected(ip!);
        //                 var robot = !string.IsNullOrWhiteSpace(ip) ? robots.GetByIp(ip!) : null;
        //                 if (robot != null)
        //                 {
        //                     var dto = RobotMapper.ToDto(robot);
        //                     _hub.Clients.All.SendAsync("telemetry", dto, stoppingToken);
        //                 }
        //                 _logger.LogInformation("Robot disconnected: {Name} {Ip}", name, ip);
        //             }
        //         }
        //         catch (Exception ex)
        //         {
        //             _logger.LogWarning(ex, "Failed to parse command payload: {Json}", json);
        //         }
        //     });
        // }
        // catch (Exception ex)
        // {
        //     _logger.LogWarning(ex, "Failed to subscribe to command subject: {Subject}", opts.CommandSubject);
        // }

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
            var timeout = TimeSpan.FromSeconds(_options.Value.DisconnectTimeoutSeconds);
            var now = DateTime.UtcNow;
            foreach (var kv in _lastTelemetrySeen.ToArray())
            {
                if (now - kv.Value > timeout)
                {
                    using var scope = _scopeFactory.CreateScope();
                    var robots = scope.ServiceProvider.GetRequiredService<IRobotService>();
                    robots.MarkDisconnected(kv.Key);
                    _lastTelemetrySeen.TryRemove(kv.Key, out _);
                    var robot = robots.GetByIp(kv.Key);
                    if (robot != null)
                    {
                        var dto = RobotMapper.ToDto(robot);
                        _hub.Clients.All.SendAsync("telemetry", dto, stoppingToken);
                    }
                    _logger.LogInformation("Robot timed out and marked disconnected: {Ip}", kv.Key);
                }
            }
        }
    }

    private static string? GetString(JsonElement root, params string[] keys)
    {
        foreach (var k in keys)
        {
            if (root.TryGetProperty(k, out var v))
            {
                return v.ValueKind == JsonValueKind.String ? v.GetString() : v.ToString();
            }
        }
        return null;
    }

    private static double? GetDouble(JsonElement root, params string[] keys)
    {
        foreach (var k in keys)
        {
            if (root.TryGetProperty(k, out var v))
            {
                if (v.ValueKind == JsonValueKind.Number) return v.GetDouble();
                if (v.ValueKind == JsonValueKind.String && double.TryParse(v.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var d)) return d;
            }
        }
        return null;
    }
}

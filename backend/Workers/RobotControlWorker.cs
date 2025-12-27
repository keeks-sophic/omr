using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using backend.Options;
using backend.Services;
using System.Collections.Concurrent;
using NATS.Client;

namespace backend.Workers;

public class RobotControlWorker : BackgroundService
{
    private readonly ILogger<RobotControlWorker> _logger;
    private readonly NatsService _nats;
    private readonly IOptions<NatsOptions> _options;
    private readonly IOptions<backend.Options.ControlOptions> _control;
    private IAsyncSubscription? _cmdSub;
    private readonly ConcurrentDictionary<string, ControlState> _state = new(StringComparer.OrdinalIgnoreCase);

    public RobotControlWorker(ILogger<RobotControlWorker> logger, NatsService nats, IOptions<NatsOptions> options, IOptions<backend.Options.ControlOptions> control)
    {
        _logger = logger;
        _nats = nats;
        _options = options;
        _control = control;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_control.Value.EnableSimulation)
        {
            _logger.LogInformation("RobotControlWorker disabled: simulation is off");
            return;
        }
        var opts = _options.Value;
        await _nats.ConnectAsync(opts.NatsUrl, stoppingToken);
        await _nats.EnsureStreamAsync(opts.TelemetryStream!, opts.TelemetrySubject);
        await _nats.EnsureStreamAsync(opts.CommandStream!, opts.CommandSubject);

        try
        {
            _cmdSub = _nats.Subscribe(opts.CommandSubject, async (s, e) =>
            {
                var json = e.Message.Data != null ? Encoding.UTF8.GetString(e.Message.Data) : "{}";
                try
                {
                    var doc = JsonDocument.Parse(json);
                    var ip = GetString(doc.RootElement, "ip", "Ip");
                    var command = GetString(doc.RootElement, "command", "Command");
                    var now = DateTime.UtcNow;
                    if (string.IsNullOrWhiteSpace(ip) || string.IsNullOrWhiteSpace(command)) return;
                    var st = _state.GetOrAdd(ip!, _ => new ControlState { Ip = ip!, X = 0, Y = 0, Battery = 50, LastChargeAt = now, LastCommandAt = now });
                    st.LastCommandAt = now;
                    switch (command!.ToLowerInvariant())
                    {
                        case "moveup":
                            st.Y += 0.1;
                            st.State = "moving";
                            break;
                        case "movedown":
                            st.Y -= 0.1;
                            st.State = "moving";
                            break;
                        case "moveleft":
                            st.X -= 0.1;
                            st.State = "moving";
                            break;
                        case "moveright":
                            st.X += 0.1;
                            st.State = "moving";
                            break;
                        case "charge":
                            st.Battery = Math.Min(100, st.Battery + 1);
                            st.State = "charging";
                            st.LastChargeAt = now;
                            break;
                        default:
                            st.State = "idle";
                            break;
                    }
                    await PublishTelemetryAsync(st);
                    _logger.LogInformation("Control command applied: {Ip} {Command} x={X} y={Y} batt={Battery}", st.Ip, command, st.X, st.Y, st.Battery);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to handle command payload: {Json}", json);
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to subscribe to command subject: {Subject}", opts.CommandSubject);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }

    private async Task PublishTelemetryAsync(ControlState st)
    {
        var payload = new
        {
            Ip = st.Ip,
            X = st.X,
            Y = st.Y,
            State = st.State,
            Battery = st.Battery,
            Connected = true,
            LastActive = DateTime.UtcNow
        };
        await _nats.PublishJsonAsync(_options.Value.TelemetrySubject, payload);
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

    private class ControlState
    {
        public string Ip { get; set; } = string.Empty;
        public double X { get; set; }
        public double Y { get; set; }
        public double Battery { get; set; }
        public string State { get; set; } = "idle";
        public DateTime LastChargeAt { get; set; }
        public DateTime LastCommandAt { get; set; }
    }
}

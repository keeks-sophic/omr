using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Robot.Options;

namespace Robot.Services;

public class TelemetryService
{
    private readonly ILogger<TelemetryService> _logger;
    private readonly NatsService _nats;
    private readonly IOptions<RobotOptions> _options;
    private string _name = "";
    private string _ip = "";
    private double _x;
    private double _y;
    private double _battery = 50;
    private string _state = "idle";
    private DateTime _lastChargeAt = DateTime.UtcNow;
    private DateTime _lastCommandAt = DateTime.UtcNow;
    private DateTime _lastAutoDecrementAt = DateTime.MinValue;
    private int? _mapId = null;
    private int[] _routeNodes = Array.Empty<int>();
    private int _routeIndex = 0;
    private List<(double x, double y)> _waypoints = new();
    private DateTime _lastMoveAt = DateTime.UtcNow;
    private double _speedMps = 0.1;

    public TelemetryService(ILogger<TelemetryService> logger, NatsService nats, IOptions<RobotOptions> options)
    {
        _logger = logger;
        _nats = nats;
        _options = options;
    }

    public void Initialize(string name, string ip)
    {
        _name = name;
        _ip = ip;
        _x = 0;
        _y = 0;
        _battery = 50;
        _state = "idle";
        _lastChargeAt = DateTime.UtcNow;
        _lastCommandAt = DateTime.UtcNow;
        _lastAutoDecrementAt = DateTime.MinValue;
        _mapId = null;
    }

    public void ApplyCommand(string command)
    {
        var now = DateTime.UtcNow;
        _lastCommandAt = now;
        switch (command.ToLowerInvariant())
        {
            case "moveup":
                _y += 0.1;
                _state = "moving";
                break;
            case "movedown":
                _y -= 0.1;
                _state = "moving";
                break;
            case "moveleft":
                _x -= 0.1;
                _state = "moving";
                break;
            case "moveright":
                _x += 0.1;
                _state = "moving";
                break;
            case "charge":
                _battery = Math.Min(100, _battery + 1);
                _state = "charging";
                _lastChargeAt = now;
                break;
            default:
                _state = "idle";
                break;
        }
    }

    public void SetRoute(int[] nodeIds)
    {
        _routeNodes = nodeIds ?? Array.Empty<int>();
        _routeIndex = 0;
        _state = "route_assigned";
    }

    public void SetMap(int? mapId)
    {
        _mapId = mapId;
    }

    public void ApplySnapshot(string? name, int? mapId, double? x, double? y, double? battery, string? state)
    {
        if (!string.IsNullOrWhiteSpace(name)) _name = name!;
        _mapId = mapId;
        if (x.HasValue) _x = x.Value;
        if (y.HasValue) _y = y.Value;
        if (battery.HasValue) _battery = battery.Value;
        if (!string.IsNullOrWhiteSpace(state)) _state = state!;
    }

    public void SetRoutePath(IEnumerable<(double x, double y)> points, double? speed, int? mapId)
    {
        _waypoints = points?.ToList() ?? new List<(double x, double y)>();
        _routeIndex = 0;
        if (speed.HasValue && speed.Value > 0) _speedMps = speed.Value;
        if (mapId.HasValue) _mapId = mapId.Value;
        _state = _waypoints.Count > 0 ? "moving" : "idle";
        _lastMoveAt = DateTime.UtcNow;
        if (_waypoints.Count > 0)
        {
            _x = _waypoints[0].x;
            _y = _waypoints[0].y;
        }
    }

    public void TickRoute()
    {
        if (_waypoints.Count == 0 || _routeIndex >= _waypoints.Count) return;
        var now = DateTime.UtcNow;
        var dt = (now - _lastMoveAt).TotalSeconds;
        if (dt <= 0) return;
        _lastMoveAt = now;
        var target = _waypoints[_routeIndex];
        var dx = target.x - _x;
        var dy = target.y - _y;
        var dist = Math.Sqrt(dx * dx + dy * dy);
        var step = _speedMps * dt;
        if (dist <= step)
        {
            _x = target.x;
            _y = target.y;
            _routeIndex++;
            if (_routeIndex >= _waypoints.Count)
            {
                _state = "idle";
                return;
            }
        }
        else
        {
            var ux = dx / dist;
            var uy = dy / dist;
            _x += ux * step;
            _y += uy * step;
            _state = "moving";
        }
    }

    public bool TickBattery()
    {
        var now = DateTime.UtcNow;
        if (now - _lastChargeAt >= TimeSpan.FromSeconds(3) && now - _lastAutoDecrementAt >= TimeSpan.FromSeconds(10))
        {
            var newBatt = Math.Max(0, _battery - 1);
            if (newBatt != _battery)
            {
                _battery = newBatt;
                _lastAutoDecrementAt = now;
                if (_state != "charging") _state = "idle";
                return true;
            }
        }
        return false;
    }

    public bool TickIdle()
    {
        var now = DateTime.UtcNow;
        if (now - _lastCommandAt >= TimeSpan.FromSeconds(2) && _state != "idle")
        {
            _state = "idle";
            return true;
        }
        return false;
    }

    public object GetStatus()
    {
        return new
        {
            Name = _name,
            Ip = _ip,
            Battery = _battery,
            X = _x,
            Y = _y,
            State = _state,
            MapId = _mapId,
            Connected = true,
            LastActive = DateTime.UtcNow
        };
    }

    public async Task PublishStatusAsync()
    {
        var status = GetStatus();
        await _nats.PublishAsync(_options.Value.TelemetrySubject, status, default);
        _logger.LogInformation("Telemetry: {@Status}", status);
    }
}

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
    private double _speedMps = 2.0;
    private double? _speedCapMps = null;
    private bool _moveAllowed = false;
    private double? _segmentLimitMeters = null;
    private double _segmentAdvancedMeters = 0;
    private int _lastSegmentIndexPublished = -1;
    private int? _robotId = null;
    private DateTime _lastSegmentRequestAt = DateTime.MinValue;

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
        _moveAllowed = false;
        _lastSegmentIndexPublished = -1;
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

    public void SetRobotId(int id)
    {
        _robotId = id;
    }

    public void SetRoutePath(IEnumerable<(double x, double y)> points, double? speed, int? mapId)
    {
        _waypoints = points?.ToList() ?? new List<(double x, double y)>();
        _routeIndex = 0;
        if (speed.HasValue && speed.Value > 0) _speedMps = speed.Value;
        if (mapId.HasValue) _mapId = mapId.Value;
        _state = _waypoints.Count > 0 ? "moving" : "idle";
        _lastMoveAt = DateTime.UtcNow;
        _lastSegmentIndexPublished = -1;
        _segmentAdvancedMeters = 0;
        _lastSegmentRequestAt = DateTime.MinValue;
        PublishNextSegmentAsync().ConfigureAwait(false);
    }

    public void TickRoute()
    {
        if (!_moveAllowed)
        {
            if (_waypoints.Count == 0 || _routeIndex >= _waypoints.Count)
            {
                _state = "idle";
                return;
            }
            var now2 = DateTime.UtcNow;
            if (now2 - _lastSegmentRequestAt >= TimeSpan.FromSeconds(1))
            {
                _lastSegmentRequestAt = now2;
                PublishNextSegmentAsync().ConfigureAwait(false);
            }
            return;
        }
        if (_waypoints.Count == 0 || _routeIndex >= _waypoints.Count) return;
        var now = DateTime.UtcNow;
        var dt = (now - _lastMoveAt).TotalSeconds;
        if (dt <= 0) return;
        _lastMoveAt = now;
        var effectiveSpeed = _speedMps;
        if (_speedCapMps.HasValue && _speedCapMps.Value > 0) effectiveSpeed = Math.Min(effectiveSpeed, _speedCapMps.Value);
        var target = _waypoints[_routeIndex];
        var dx = target.x - _x;
        var dy = target.y - _y;
        var dist = Math.Sqrt(dx * dx + dy * dy);
        var step = effectiveSpeed * dt;
        if (_segmentLimitMeters.HasValue)
        {
            var remaining = Math.Max(0, _segmentLimitMeters.Value - _segmentAdvancedMeters);
            step = Math.Min(step, remaining);
        }
        if (dist <= step)
        {
            _x = target.x;
            _y = target.y;
            _routeIndex++;
            _segmentAdvancedMeters += dist;
            _segmentLimitMeters = null;
            if (_routeIndex != _lastSegmentIndexPublished)
            {
                _segmentAdvancedMeters = 0;
                PublishNextSegmentAsync().ConfigureAwait(false);
            }
            if (_routeIndex >= _waypoints.Count)
            {
                _state = "idle";
                _waypoints.Clear();
                _routeIndex = 0;
                _segmentLimitMeters = null;
                _moveAllowed = false;
                PublishStatusAsync().ConfigureAwait(false);
                return;
            }
        }
        else
        {
            var ux = dx / dist;
            var uy = dy / dist;
            _x += ux * step;
            _y += uy * step;
            _segmentAdvancedMeters += step;
            _state = "moving";
        }
        if (_segmentLimitMeters.HasValue && _segmentAdvancedMeters >= _segmentLimitMeters.Value - 1e-6)
        {
            _segmentLimitMeters = null;
            _segmentAdvancedMeters = 0;
            PublishNextSegmentAsync().ConfigureAwait(false);
        }
    }

    public void SetTrafficControl(bool allowed, double? limitMeters = null, double? speedLimit = null)
    {
        _moveAllowed = allowed;
        _segmentLimitMeters = limitMeters;
        if (speedLimit.HasValue && speedLimit.Value > 0) _speedCapMps = speedLimit.Value;
        else _speedCapMps = null;
        if (!allowed) _state = (_waypoints.Count == 0 || _routeIndex >= _waypoints.Count) ? "idle" : "stop";
        else if (_waypoints.Count > 0) _state = "moving";
    }

    private async Task PublishNextSegmentAsync()
    {
        if (_waypoints.Count == 0) return;
        var points = new List<object>();
        var idx = Math.Max(0, _routeIndex);
        var lastX = _x;
        var lastY = _y;
        double accum = 0.0;
        for (var i = idx; i < _waypoints.Count; i++)
        {
            var p = _waypoints[i];
            var dx = p.x - lastX;
            var dy = p.y - lastY;
            var d = Math.Sqrt(dx * dx + dy * dy);
            accum += d;
            points.Add(new { x = p.x, y = p.y });
            lastX = p.x;
            lastY = p.y;
            if (accum >= 5.0) break;
        }
        var payload = new
        {
            command = "route.segment",
            ip = _ip,
            segment = new
            {
                mapId = _mapId,
                points = points.ToArray(),
                length = accum
            }
        };
        var subject = $"{Robot.Topics.NatsSubjects.RouteSegmentPrefix}.{(_robotId.HasValue ? _robotId.Value : _ip)}";
        await _nats.PublishAsync(subject, payload, default);
        _lastSegmentIndexPublished = _routeIndex;
        _segmentAdvancedMeters = 0;
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
                return true;
            }
        }
        return false;
    }

    public bool TickIdle()
    {
        var now = DateTime.UtcNow;
        var hasActiveRoute = _waypoints.Count > 0 && _routeIndex < _waypoints.Count;
        var isMovingOrStopped = string.Equals(_state, "moving", StringComparison.OrdinalIgnoreCase)
            || string.Equals(_state, "stop", StringComparison.OrdinalIgnoreCase)
            || string.Equals(_state, "charging", StringComparison.OrdinalIgnoreCase);
        if (now - _lastCommandAt >= TimeSpan.FromSeconds(2) && !hasActiveRoute && !isMovingOrStopped && _state != "idle")
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
        var subject = $"{Robot.Topics.NatsSubjects.TelemetryPrefix}.{(_robotId.HasValue ? _robotId.Value : _ip)}";
        await _nats.PublishAsync(subject, status, default);
        _logger.LogInformation("Telemetry: {@Status}", status);
    }
}

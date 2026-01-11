using System;
using System.Threading;
using Robot.Services;
using Robot.Options;

namespace Robot.Workers;

public class PeriodicScheduler
{
    private Timer? _heartbeat;
    private Timer? _snapshot;
    private Timer? _motion;
    private Timer? _route;
    private Timer? _battery;
    private Timer? _health;
    private readonly HeartbeatWorker _heartbeatWorker;
    private readonly StateSnapshotWorker _snapshotWorker;
    private readonly MotionTickWorker _motionWorker;
    private readonly Robot.Domain.TaskRoute.TaskRouteExecutor _routeExecutor;
    private readonly TelemetryService _telemetry;
    private readonly TickOptions _ticks;
    public PeriodicScheduler(HeartbeatWorker hb, StateSnapshotWorker ss, MotionTickWorker mw, Robot.Domain.TaskRoute.TaskRouteExecutor re, TelemetryService telemetry, TickOptions ticks)
    {
        _heartbeatWorker = hb;
        _snapshotWorker = ss;
        _motionWorker = mw;
        _routeExecutor = re;
        _telemetry = telemetry;
        _ticks = ticks;
    }
    public void Start(string robotId)
    {
        _heartbeat = new Timer(_ => _heartbeatWorker.SendHeartbeat(robotId, 0, null), null, TimeSpan.FromSeconds(_ticks.HeartbeatSeconds), TimeSpan.FromSeconds(_ticks.HeartbeatSeconds));
        _snapshot = new Timer(_ => _snapshotWorker.PublishSnapshot(robotId), null, TimeSpan.FromSeconds(_ticks.SnapshotSeconds), TimeSpan.FromSeconds(_ticks.SnapshotSeconds));
        _motion = new Timer(_ => _motionWorker.Tick(robotId), null, TimeSpan.FromMilliseconds(_ticks.MotionMs), TimeSpan.FromMilliseconds(_ticks.MotionMs));
        _route = new Timer(_ => _routeExecutor.Tick(robotId), null, TimeSpan.FromSeconds(_ticks.RouteSeconds), TimeSpan.FromSeconds(_ticks.RouteSeconds));
        _battery = new Timer(_ => _telemetry.PublishBattery(robotId), null, TimeSpan.FromSeconds(_ticks.BatterySeconds), TimeSpan.FromSeconds(_ticks.BatterySeconds));
        _health = new Timer(_ => _telemetry.PublishHealth(robotId), null, TimeSpan.FromSeconds(_ticks.HealthSeconds), TimeSpan.FromSeconds(_ticks.HealthSeconds));
    }
    public void Stop()
    {
        _heartbeat?.Dispose();
        _snapshot?.Dispose();
        _motion?.Dispose();
        _route?.Dispose();
        _battery?.Dispose();
        _health?.Dispose();
    }
}

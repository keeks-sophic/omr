using System;
using Robot.Domain.State;
using Robot.Workers;
using Robot.Services;

namespace Robot.Domain.Hardware.Drivers;

public class RadarDriver
{
    private readonly RobotStateStore _store;
    private readonly StateSnapshotWorker _snapshot;
    private readonly TelemetryService _telemetry;
    public RadarDriver(RobotStateStore store, StateSnapshotWorker snapshot, TelemetryService telemetry)
    {
        _store = store;
        _snapshot = snapshot;
        _telemetry = telemetry;
    }
    public void SetObstacleDetected(bool detected, string robotId)
    {
        var changed = _store.Apply(new RadarObstacleChanged { ObstacleDetected = detected, Timestamp = DateTimeOffset.UtcNow, Source = "Radar" });
        _snapshot.PublishEvent(robotId, changed);
        _telemetry.PublishRadar(robotId);
    }
}

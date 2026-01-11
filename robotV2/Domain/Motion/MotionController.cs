using System;
using Robot.Domain.State;
using Robot.Domain.Hardware.Drivers;
using Robot.Workers;
using Robot.Services;

namespace Robot.Domain.Motion;

public class MotionController
{
    private readonly RobotStateStore _store;
    private readonly DriveDriver _drive;
    private readonly StateSnapshotWorker _snapshot;
    private readonly TelemetryService _telemetry;
    private readonly double _tickSeconds;
    public MotionController(RobotStateStore store, DriveDriver drive, StateSnapshotWorker snapshot, TelemetryService telemetry, double tickSeconds = 0.05)
    {
        _store = store;
        _drive = drive;
        _snapshot = snapshot;
        _telemetry = telemetry;
        _tickSeconds = tickSeconds;
    }
    public void Tick(string robotId)
    {
        var target = _store.State.Motion.TargetLinearVel;
        var limits = _store.State.MotionLimits;
        // Clamp
        var clampedTarget = Math.Max(-limits.MaxDriveSpeed, Math.Min(limits.MaxDriveSpeed, target));
        // Safety override
        if (_store.State.Safety.ObstacleDetected || _store.State.Safety.EstopActive)
        {
            clampedTarget = 0;
        }
        var current = _store.State.Motion.CurrentLinearVel;
        double newCurrent;
        if (clampedTarget > current)
        {
            var maxDelta = limits.MaxAcceleration * _tickSeconds;
            newCurrent = current + Math.Min(maxDelta, clampedTarget - current);
        }
        else
        {
            var maxDelta = limits.MaxDeceleration * _tickSeconds;
            var delta = clampedTarget - current;
            newCurrent = current + Math.Max(-maxDelta, delta);
        }
        _drive.SetLinearSetpoint(newCurrent);
        var changed = _store.Apply(new MotionCurrentUpdated { CurrentLinearVel = newCurrent });
        _snapshot.PublishEvent(robotId, changed);
        _telemetry.PublishMotion(robotId);
    }
}

using System;
using Robot.Domain.State;
using Robot.Domain.Motion;

namespace Robot.Workers;

public class MotionTickWorker
{
    private readonly RobotStateStore _store;
    private readonly Robot.Domain.Traffic.TrafficAdapter _traffic;
    private readonly StateSnapshotWorker _snapshot;
    private readonly MotionController _controller;
    public MotionTickWorker(RobotStateStore store, Robot.Domain.Traffic.TrafficAdapter traffic, StateSnapshotWorker snapshot, MotionController controller)
    {
        _store = store;
        _traffic = traffic;
        _snapshot = snapshot;
        _controller = controller;
    }
    public void Tick(string robotId)
    {
        var now = DateTimeOffset.UtcNow;
        var target = _traffic.GetCurrentTargetVel(now);
        if (_traffic.IsStale(now, 2000)) target = 0;
        if (_store.State.Safety.ObstacleDetected) target = 0;
        var changed = _store.Apply(new MotionTargetUpdated { TargetLinearVel = target });
        _snapshot.PublishEvent(robotId, changed);
        _controller.Tick(robotId);
    }
}

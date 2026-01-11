using System;
using Robot.Domain.State;
using Robot.Workers;

namespace Robot.Domain.Hardware.Safety;

public class SafetyInterlocks
{
    private readonly RobotStateStore _store;
    private readonly StateSnapshotWorker _snapshot;
    public SafetyInterlocks(RobotStateStore store, StateSnapshotWorker snapshot)
    {
        _store = store;
        _snapshot = snapshot;
    }
    public void SetEstop(string robotId, bool active)
    {
        var changed = _store.Apply(new EStopChanged { EstopActive = active, Timestamp = DateTimeOffset.UtcNow, Source = "SafetyInterlocks" });
        _snapshot.PublishEvent(robotId, changed);
    }
    public void RaiseFault(string robotId, string reason)
    {
        var changed = _store.Apply(new FaultRaised { Reason = reason, Timestamp = DateTimeOffset.UtcNow, Source = "SafetyInterlocks" });
        _snapshot.PublishEvent(robotId, changed);
    }
    public void ClearFault(string robotId)
    {
        var changed = _store.Apply(new FaultCleared { Timestamp = DateTimeOffset.UtcNow, Source = "SafetyInterlocks" });
        _snapshot.PublishEvent(robotId, changed);
    }
}

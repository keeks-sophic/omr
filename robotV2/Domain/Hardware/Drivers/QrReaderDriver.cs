using System;
using Robot.Domain.State;
using Robot.Workers;
using Robot.Services;

namespace Robot.Domain.Hardware.Drivers;

public class QrReaderDriver
{
    private readonly RobotStateStore _store;
    private readonly StateSnapshotWorker _snapshot;
    private readonly TelemetryService _telemetry;
    public QrReaderDriver(RobotStateStore store, StateSnapshotWorker snapshot, TelemetryService telemetry)
    {
        _store = store;
        _snapshot = snapshot;
        _telemetry = telemetry;
    }
    public void OnScan(string robotId, string code)
    {
        var changed = _store.Apply(new QrScanned { Code = code, Timestamp = DateTimeOffset.UtcNow, Source = "QrReader" });
        _snapshot.PublishEvent(robotId, changed);
        _telemetry.PublishQr(robotId);
    }
}

using BackendV2.Api.Contracts.State;
using BackendV2.Api.Contracts.Telemetry;

namespace BackendV2.Api.Service.Ingestion;

public static class IngestionValidator
{
    public static bool Validate(RobotStateSnapshot snap)
    {
        return !string.IsNullOrWhiteSpace(snap.RobotId) && snap.Timestamp != default;
    }
    public static bool Validate(RobotStateEvent evt)
    {
        return !string.IsNullOrWhiteSpace(evt.RobotId) && evt.Timestamp != default && !string.IsNullOrWhiteSpace(evt.EventType);
    }
    public static bool Validate(BatteryTelemetry t) => t.BatteryPct >= 0 && t.BatteryPct <= 100;
    public static bool Validate(HealthTelemetry t) => true;
    public static bool Validate(PoseTelemetry t) => true;
    public static bool Validate(MotionTelemetry t) => true;
    public static bool Validate(RadarTelemetry t) => t.Distance >= 0;
    public static bool Validate(QrTelemetry t) => !string.IsNullOrWhiteSpace(t.QrCode);
}

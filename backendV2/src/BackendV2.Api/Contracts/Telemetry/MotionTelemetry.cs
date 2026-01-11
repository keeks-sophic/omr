namespace BackendV2.Api.Contracts.Telemetry;

public class MotionTelemetry
{
    public double CurrentLinearVel { get; set; }
    public double TargetLinearVel { get; set; }
    public string MotionState { get; set; } = string.Empty;
}

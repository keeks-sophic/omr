using System;

namespace Robot.Contracts.Telemetry;

public class MotionTelemetry
{
    public string RobotId { get; set; } = "";
    public double CurrentLinearVel { get; set; }
    public double TargetLinearVel { get; set; }
    public string MotionState { get; set; } = "";
    public DateTimeOffset Timestamp { get; set; }
}

using System;

namespace Robot.Contracts.Telemetry;

public class HealthTelemetry
{
    public string RobotId { get; set; } = "";
    public double? Temperature { get; set; }
    public string[]? MotorFaults { get; set; }
    public string? LastError { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}

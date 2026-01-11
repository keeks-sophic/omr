using System;

namespace Robot.Contracts.Telemetry;

public class RadarTelemetry
{
    public string RobotId { get; set; } = "";
    public bool ObstacleDetected { get; set; }
    public double? Distance { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}

using System;

namespace Robot.Contracts.Telemetry;

public class PoseTelemetry
{
    public string RobotId { get; set; } = "";
    public double X { get; set; }
    public double Y { get; set; }
    public double? Heading { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}

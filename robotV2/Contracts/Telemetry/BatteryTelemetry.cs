using System;

namespace Robot.Contracts.Telemetry;

public class BatteryTelemetry
{
    public string RobotId { get; set; } = "";
    public double BatteryPct { get; set; }
    public double? Voltage { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}

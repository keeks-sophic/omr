using System;

namespace Robot.Contracts.Telemetry;

public class QrTelemetry
{
    public string RobotId { get; set; } = "";
    public string QrCode { get; set; } = "";
    public DateTimeOffset ScannedAt { get; set; }
}

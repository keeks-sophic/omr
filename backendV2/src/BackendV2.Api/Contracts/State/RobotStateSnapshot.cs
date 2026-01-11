using System;

namespace BackendV2.Api.Contracts.State;

public class RobotStateSnapshot
{
    public string RobotId { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }
    public string Mode { get; set; } = "IDLE";
    public string RuntimeMode { get; set; } = "LIVE";
    public double BatteryPct { get; set; }
    public string? LastQrCode { get; set; }
}

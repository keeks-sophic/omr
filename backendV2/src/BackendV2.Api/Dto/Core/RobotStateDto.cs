using System;

namespace BackendV2.Api.Dto.Core;

public class RobotStateDto
{
    public string RobotId { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public string Mode { get; set; } = "IDLE";
    public double BatteryPct { get; set; }
}

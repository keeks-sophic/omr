using System;

namespace BackendV2.Api.Dto.Replay;

public class ReplayCreateRequest
{
    public string RobotId { get; set; } = string.Empty;
    public DateTimeOffset FromTime { get; set; }
    public DateTimeOffset ToTime { get; set; }
    public double PlaybackSpeed { get; set; } = 1.0;
}

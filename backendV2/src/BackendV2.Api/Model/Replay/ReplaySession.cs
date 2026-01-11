using System;

namespace BackendV2.Api.Model.Replay;

public class ReplaySession
{
    public Guid ReplaySessionId { get; set; }
    public string RobotId { get; set; } = string.Empty;
    public DateTimeOffset FromTime { get; set; }
    public DateTimeOffset ToTime { get; set; }
    public double PlaybackSpeed { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

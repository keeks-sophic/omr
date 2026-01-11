using System;

namespace BackendV2.Api.Model.Replay;

public class RobotEvent
{
    public Guid EventId { get; set; }
    public string RobotId { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
}


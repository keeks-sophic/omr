using System;

namespace BackendV2.Api.Contracts.State;

public class RobotStateEvent
{
    public string RobotId { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? DetailsJson { get; set; }
}

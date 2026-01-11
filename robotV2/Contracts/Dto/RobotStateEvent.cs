using System;

namespace Robot.Contracts.Dto;

public class RobotStateEvent
{
    public string RobotId { get; set; } = "";
    public DateTimeOffset Timestamp { get; set; }
    public string[] ChangedFields { get; set; } = System.Array.Empty<string>();
    public RobotStateDto State { get; set; } = new();
}


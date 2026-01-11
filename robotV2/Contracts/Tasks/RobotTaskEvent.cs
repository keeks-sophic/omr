using System;

namespace Robot.Contracts.Tasks;

public class RobotTaskEvent
{
    public string RobotId { get; set; } = "";
    public string TaskId { get; set; } = "";
    public string Status { get; set; } = "";
    public string? Detail { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}

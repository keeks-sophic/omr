using System;

namespace Robot.Contracts.Logs;

public class RobotLogEvent
{
    public string RobotId { get; set; } = "";
    public string Level { get; set; } = "INFO";
    public string Message { get; set; } = "";
    public string? Detail { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public string? CorrelationId { get; set; }
}

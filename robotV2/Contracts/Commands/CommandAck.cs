using System;

namespace Robot.Contracts.Commands;

public class CommandAck
{
    public string RobotId { get; set; } = "";
    public string CorrelationId { get; set; } = "";
    public string Status { get; set; } = "ACK";
    public string? Reason { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}

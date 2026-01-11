using System;

namespace BackendV2.Api.Contracts.Commands;

public class CommandAck
{
    public string RobotId { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}

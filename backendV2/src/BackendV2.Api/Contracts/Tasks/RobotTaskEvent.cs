using System;

namespace BackendV2.Api.Contracts.Tasks;

public class RobotTaskEvent
{
    public string TaskId { get; set; } = "";
    public string Status { get; set; } = "";
    public string? Detail { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}


using System;

namespace BackendV2.Api.Model.Task;

public class TaskEvent
{
    public Guid TaskEventId { get; set; }
    public Guid TaskId { get; set; }
    public string? RobotId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Detail { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string? PayloadJson { get; set; }
}

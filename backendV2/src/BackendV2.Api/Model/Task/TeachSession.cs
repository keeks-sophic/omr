using System;

namespace BackendV2.Api.Model.Task;

public class TeachSession
{
    public Guid TeachSessionId { get; set; }
    public string RobotId { get; set; } = string.Empty;
    public Guid MapVersionId { get; set; }
    public Guid? CreatedBy { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? StoppedAt { get; set; }
    public string CapturedStepsJson { get; set; } = "[]";
}

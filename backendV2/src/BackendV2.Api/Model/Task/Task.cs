using System;

namespace BackendV2.Api.Model.Task;

public class Task
{
    public Guid TaskId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public int Priority { get; set; }
    public string Status { get; set; } = "ASSIGNED";
    public string AssignmentMode { get; set; } = "AUTO";
    public string? RobotId { get; set; }
    public Guid MapVersionId { get; set; }
    public string TaskType { get; set; } = string.Empty;
    public string ParametersJson { get; set; } = "{}";
    public Guid? MissionId { get; set; }
    public Guid? CurrentRouteId { get; set; }
    public DateTimeOffset? Eta { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

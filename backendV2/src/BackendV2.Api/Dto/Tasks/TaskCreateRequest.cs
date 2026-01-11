using System;

namespace BackendV2.Api.Dto.Tasks;

public class TaskCreateRequest
{
    public string TaskType { get; set; } = string.Empty;
    public Guid MapVersionId { get; set; }
    public int Priority { get; set; } = 0;
    public string AssignmentMode { get; set; } = "AUTO";
    public string? RobotId { get; set; }
    public object Parameters { get; set; } = new { };
}

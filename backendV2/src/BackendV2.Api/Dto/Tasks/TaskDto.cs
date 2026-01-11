using System;

namespace BackendV2.Api.Dto.Tasks;

public class TaskDto
{
    public Guid TaskId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public string Status { get; set; } = string.Empty;
    public string AssignmentMode { get; set; } = string.Empty;
    public string? RobotId { get; set; }
    public Guid MapVersionId { get; set; }
    public string TaskType { get; set; } = string.Empty;
    public object Parameters { get; set; } = new object();
    public Guid? MissionId { get; set; }
    public Guid? CurrentRouteId { get; set; }
    public DateTimeOffset? Eta { get; set; }
}

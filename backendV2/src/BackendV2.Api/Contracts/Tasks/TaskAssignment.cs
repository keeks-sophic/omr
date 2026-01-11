using System;

namespace BackendV2.Api.Contracts.Tasks;

public class TaskAssignment
{
    public string TaskId { get; set; } = string.Empty;
    public string TaskType { get; set; } = string.Empty;
    public object Parameters { get; set; } = new object();
    public string? MissionId { get; set; }
    public string MapVersionId { get; set; } = string.Empty;
}

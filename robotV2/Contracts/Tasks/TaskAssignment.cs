namespace Robot.Contracts.Tasks;

public class TaskAssignment
{
    public string CorrelationId { get; set; } = "";
    public string TaskId { get; set; } = "";
    public string TaskType { get; set; } = "";
    public object? Parameters { get; set; }
    public string? MissionId { get; set; }
    public string MapVersionId { get; set; } = "";
}

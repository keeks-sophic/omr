namespace Robot.Domain.TaskRoute;

public class ActiveTask
{
    public string TaskId { get; set; } = "";
    public string TaskType { get; set; } = "";
    public string Status { get; set; } = "NONE";
    public object? Parameters { get; set; }
}


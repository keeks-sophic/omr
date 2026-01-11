namespace BackendV2.Api.Contracts.Logs;

public class RobotLogEvent
{
    public string Level { get; set; } = "INFO";
    public string Message { get; set; } = string.Empty;
    public string? Detail { get; set; }
}

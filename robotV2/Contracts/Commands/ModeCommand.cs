namespace Robot.Contracts.Commands;

public class ModeCommand
{
    public string CorrelationId { get; set; } = "";
    public string Mode { get; set; } = "";
    public bool? TeachEnabled { get; set; }
    public string? TeachSessionId { get; set; }
}

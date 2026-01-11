namespace Robot.Contracts.Commands;

public class CamToggleCommand
{
    public string CorrelationId { get; set; } = "";
    public string CamSide { get; set; } = "";
}

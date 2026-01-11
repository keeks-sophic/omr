namespace Robot.Contracts.Commands;

public class RotateCommand
{
    public string CorrelationId { get; set; } = "";
    public string Mode { get; set; } = "";
    public double Value { get; set; }
}

namespace Robot.Contracts.Commands;

public class HoistCommand
{
    public string CorrelationId { get; set; } = "";
    public string Mode { get; set; } = "";
    public double Value { get; set; }
}

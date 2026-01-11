namespace Robot.Contracts.Commands;

public class TelescopeCommand
{
    public string CorrelationId { get; set; } = "";
    public string Mode { get; set; } = "";
    public double Value { get; set; }
}

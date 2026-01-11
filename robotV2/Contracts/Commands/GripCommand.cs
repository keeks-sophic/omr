namespace Robot.Contracts.Commands;

public class GripCommand
{
    public string CorrelationId { get; set; } = "";
    public string Action { get; set; } = "";
}

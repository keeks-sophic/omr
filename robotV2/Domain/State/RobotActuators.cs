namespace Robot.Domain.State;

public class RobotActuators
{
    public double? HoistPosition { get; set; }
    public double? TelescopePosition { get; set; }
    public string? GripState { get; set; }
    public double? RotatePosition { get; set; }
}


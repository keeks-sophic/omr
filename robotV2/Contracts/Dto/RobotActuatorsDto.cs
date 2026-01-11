namespace Robot.Contracts.Dto;

public class RobotActuatorsDto
{
    public double? HoistPosition { get; set; }
    public double? TelescopePosition { get; set; }
    public string? GripState { get; set; }
    public double? RotatePosition { get; set; }
}


namespace Robot.Domain.State;

public class SafetyState
{
    public bool ObstacleDetected { get; set; }
    public bool EstopActive { get; set; }
    public string SafetyStopReason { get; set; } = "NONE";
}


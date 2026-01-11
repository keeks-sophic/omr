namespace Robot.Domain.Motion;

public class MotionState
{
    public string MotionStateName { get; set; } = "STOPPED";
    public double CurrentLinearVel { get; set; }
    public double TargetLinearVel { get; set; }
    public string CamSide { get; set; } = "CENTER";
}


using Robot.Domain.State;

namespace Robot.Domain.Commands;

public class CommandExecutor
{
    private readonly RobotActuators _actuators;
    public CommandExecutor(RobotActuators actuators)
    {
        _actuators = actuators;
    }
    public void SetGrip(string action)
    {
        _actuators.GripState = action;
    }
    public void SetHoist(double value)
    {
        _actuators.HoistPosition = value;
    }
    public void SetTelescope(double value)
    {
        _actuators.TelescopePosition = value;
    }
    public void SetRotate(double value)
    {
        _actuators.RotatePosition = value;
    }
}

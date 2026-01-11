namespace Robot.Domain.Hardware.Drivers;

public class DriveDriver
{
    public double LastSetpoint { get; private set; }
    public void SetLinearSetpoint(double velocity)
    {
        LastSetpoint = velocity;
    }
}

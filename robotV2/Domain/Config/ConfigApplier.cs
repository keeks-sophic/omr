using Robot.Domain.Motion;

namespace Robot.Domain.Config;

public class ConfigApplier
{
    public void ApplyMotionLimits(MotionLimits target, MotionLimits source)
    {
        target.MaxDriveSpeed = source.MaxDriveSpeed;
        target.MaxAcceleration = source.MaxAcceleration;
        target.MaxDeceleration = source.MaxDeceleration;
    }
}

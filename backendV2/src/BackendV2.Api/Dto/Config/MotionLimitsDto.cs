namespace BackendV2.Api.Dto.Config;

public class MotionLimitsDto
{
    public double MaxLinearVel { get; set; }
    public double MaxAngularVel { get; set; }
    public double MaxAccel { get; set; }
    public double MaxDecel { get; set; }
}

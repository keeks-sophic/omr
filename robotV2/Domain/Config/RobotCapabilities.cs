namespace Robot.Domain.Config;

public class RobotCapabilities
{
    public bool SupportsCamToggle { get; set; }
    public bool SupportsRadar { get; set; }
    public bool SupportsQrReader { get; set; }
    public bool SupportsHoist { get; set; }
    public bool SupportsTelescope { get; set; }
    public bool SupportsGrip { get; set; }
    public bool SupportsRotate { get; set; }
}


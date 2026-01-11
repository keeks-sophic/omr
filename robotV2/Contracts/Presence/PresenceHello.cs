namespace Robot.Contracts.Presence;

public class PresenceHello
{
    public string RobotId { get; set; } = "";
    public string? Name { get; set; }
    public string? Ip { get; set; }
    public string SoftwareVersion { get; set; } = "";
    public string RuntimeMode { get; set; } = "LIVE";
    public Dto.RobotCapabilitiesDto Capabilities { get; set; } = new();
    public Dto.RobotFeatureFlagsDto FeatureFlags { get; set; } = new();
}

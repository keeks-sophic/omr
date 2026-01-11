using BackendV2.Api.Dto.Robots;

namespace BackendV2.Api.Contracts.Presence;

public class PresenceHello
{
    public string SoftwareVersion { get; set; } = string.Empty;
    public string RuntimeMode { get; set; } = string.Empty;
    public RobotCapabilitiesDto Capabilities { get; set; } = new RobotCapabilitiesDto();
    public BackendV2.Api.Dto.Robots.RobotFeatureFlagsDto FeatureFlags { get; set; } = new BackendV2.Api.Dto.Robots.RobotFeatureFlagsDto();
}

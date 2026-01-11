using System;

namespace BackendV2.Api.Dto.Robots;

public class RobotSessionDto
{
    public string RobotId { get; set; } = string.Empty;
    public bool Connected { get; set; }
    public DateTimeOffset LastSeen { get; set; }
    public string RuntimeMode { get; set; } = string.Empty;
    public string SoftwareVersion { get; set; } = string.Empty;
    public RobotCapabilitiesDto Capabilities { get; set; } = new RobotCapabilitiesDto();
    public RobotFeatureFlagsDto FeatureFlags { get; set; } = new RobotFeatureFlagsDto();
}

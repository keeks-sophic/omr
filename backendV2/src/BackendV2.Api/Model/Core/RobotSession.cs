using System;

namespace BackendV2.Api.Model.Core;

public class RobotSession
{
    public string RobotId { get; set; } = string.Empty;
    public bool Connected { get; set; }
    public DateTimeOffset LastSeen { get; set; }
    public string RuntimeMode { get; set; } = "LIVE";
    public string? SoftwareVersion { get; set; }
    public string CapabilitiesJson { get; set; } = "{}";
    public string FeatureFlagsJson { get; set; } = "{}";
    public string MotionLimitsJson { get; set; } = "{}";
    public DateTimeOffset UpdatedAt { get; set; }
}

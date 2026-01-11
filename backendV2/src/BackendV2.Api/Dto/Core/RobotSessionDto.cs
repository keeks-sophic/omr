using System;

namespace BackendV2.Api.Dto.Core;

public class RobotSessionDto
{
    public string RobotId { get; set; } = string.Empty;
    public bool Connected { get; set; }
    public DateTimeOffset LastSeen { get; set; }
    public string RuntimeMode { get; set; } = "LIVE";
    public string? SoftwareVersion { get; set; }
    public object Capabilities { get; set; } = new { };
    public object FeatureFlags { get; set; } = new { };
}

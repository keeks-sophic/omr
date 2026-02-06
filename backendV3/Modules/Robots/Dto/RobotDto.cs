using System.Text.Json;

namespace BackendV3.Modules.Robots.Dto;

public sealed class RobotDto
{
    public string RobotId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public DateTimeOffset? LastSeenAt { get; set; }

    public RobotIdentitySummaryDto? Identity { get; set; }
    public JsonDocument? Capability { get; set; }
    public JsonDocument? ReportedSettings { get; set; }
}

public sealed class RobotIdentitySummaryDto
{
    public string? Vendor { get; set; }
    public string? Model { get; set; }
    public string? FirmwareVersion { get; set; }
    public string? SerialNumber { get; set; }
}


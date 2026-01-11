using System;

namespace BackendV2.Api.Dto.Ops;

public class OpsAlertDto
{
    public string Type { get; set; } = string.Empty;
    public string Severity { get; set; } = "info";
    public string Message { get; set; } = string.Empty;
    public string? RobotId { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}


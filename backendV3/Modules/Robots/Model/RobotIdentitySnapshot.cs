namespace BackendV3.Modules.Robots.Model;

public sealed class RobotIdentitySnapshot
{
    public Guid SnapshotId { get; set; }
    public string RobotId { get; set; } = string.Empty;

    public string? Vendor { get; set; }
    public string? Model { get; set; }
    public string? FirmwareVersion { get; set; }
    public string? SerialNumber { get; set; }

    public string PayloadJson { get; set; } = "{}";
    public DateTimeOffset ReceivedAt { get; set; }
}


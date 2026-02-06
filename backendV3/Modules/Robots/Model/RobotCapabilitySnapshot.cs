namespace BackendV3.Modules.Robots.Model;

public sealed class RobotCapabilitySnapshot
{
    public Guid SnapshotId { get; set; }
    public string RobotId { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";
    public DateTimeOffset ReceivedAt { get; set; }
}


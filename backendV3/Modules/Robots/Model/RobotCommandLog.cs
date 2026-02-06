namespace BackendV3.Modules.Robots.Model;

public sealed class RobotCommandLog
{
    public Guid CommandId { get; set; }
    public string RobotId { get; set; } = string.Empty;
    public string CommandType { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";
    public Guid? RequestedByUserId { get; set; }
    public DateTimeOffset RequestedAt { get; set; }
    public DateTimeOffset? LastAckAt { get; set; }
    public string Status { get; set; } = "REQUESTED";
}


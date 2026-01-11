namespace Robot.Contracts.Presence;

public class PresenceHeartbeat
{
    public string RobotId { get; set; } = "";
    public long UptimeMs { get; set; }
    public string? LastError { get; set; }
}

namespace BackendV2.Api.Contracts.Presence;

public class PresenceHeartbeat
{
    public long UptimeMs { get; set; }
    public string? LastError { get; set; }
}

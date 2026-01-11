using System;

namespace BackendV2.Api.Model.Ops;

public class AuditEvent
{
    public Guid AuditEventId { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public Guid? ActorUserId { get; set; }
    public string? ActorRoles { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? TargetType { get; set; }
    public string? TargetId { get; set; }
    public string Outcome { get; set; } = string.Empty;
    public string? DetailsJson { get; set; }
}

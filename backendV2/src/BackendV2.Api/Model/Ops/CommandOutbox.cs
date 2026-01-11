using System;

namespace BackendV2.Api.Model.Ops;

public class CommandOutbox
{
    public Guid OutboxId { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string RobotId { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";
    public int RetryCount { get; set; } = 0;
    public string Status { get; set; } = "Pending";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset LastAttempt { get; set; }
}

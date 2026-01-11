using System;

namespace BackendV2.Api.SignalR;

public class RealtimeMessage<TPayload>
{
    public string Topic { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }
    public string? CorrelationId { get; set; }
    public string? RobotId { get; set; }
    public TPayload? Payload { get; set; }
}

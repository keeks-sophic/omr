using System;

namespace BackendV2.Api.Contracts;

public class NatsEnvelope<TPayload>
{
    public string RobotId { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }
    public string Source { get; set; } = "backend";
    public TPayload? Payload { get; set; }
}

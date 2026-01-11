using System;

namespace Robot.Contracts;

public class NatsEnvelope<TPayload>
{
    public string RobotId { get; set; } = "";
    public string CorrelationId { get; set; } = "";
    public DateTimeOffset Timestamp { get; set; }
    public string Source { get; set; } = "robot";
    public TPayload? Payload { get; set; }
}

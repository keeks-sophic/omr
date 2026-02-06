namespace BackendV3.Modules.Robots.Messaging;

public sealed class RobotNatsEnvelope
{
    public int PayloadVersion { get; set; } = 1;
    public Guid MessageId { get; set; } = Guid.NewGuid();
    public string RobotId { get; set; } = string.Empty;
    public DateTimeOffset SentAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid? CorrelationId { get; set; }
    public string Type { get; set; } = string.Empty;
    public object Payload { get; set; } = new { };
}


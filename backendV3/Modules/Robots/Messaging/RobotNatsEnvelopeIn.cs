using System.Text.Json;

namespace BackendV3.Modules.Robots.Messaging;

public sealed class RobotNatsEnvelopeIn
{
    public int PayloadVersion { get; set; }
    public Guid MessageId { get; set; }
    public string RobotId { get; set; } = string.Empty;
    public DateTimeOffset SentAt { get; set; }
    public Guid? CorrelationId { get; set; }
    public string Type { get; set; } = string.Empty;
    public JsonElement Payload { get; set; }
}


namespace backend.Options;

public class NatsOptions
{
    public string NatsUrl { get; set; } = "nats://localhost:4222";
    public string TelemetrySubject { get; set; } = "robot.telemetry";
    public string CommandSubject { get; set; } = "robot.command";
    public string? TelemetryStream { get; set; } = "ROBOT_TELEMETRY";
    public string? CommandStream { get; set; } = "ROBOT_COMMAND";
    public int DisconnectTimeoutSeconds { get; set; } = 3;
}

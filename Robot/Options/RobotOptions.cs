namespace Robot.Options;

public class RobotOptions
{
    public string Name { get; set; } = "Robot";
    public string? Interface { get; set; }
    public string? Ip { get; set; }
    public string NatsUrl { get; set; } = "nats://localhost:4222";
    public string TelemetrySubject { get; set; } = "robot.telemetry";
    public string CommandSubject { get; set; } = "robot.command";
    public string? TelemetryStream { get; set; } = "ROBOT_TELEMETRY";
    public string? CommandStream { get; set; } = "ROBOT_COMMAND";

}

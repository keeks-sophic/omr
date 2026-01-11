using Robot.Services;

namespace Robot.Workers;

public class RobotWorker
{
    private readonly CommandListenerService _commands;
    private readonly TelemetryService _telemetry;
    public RobotWorker(CommandListenerService commands, TelemetryService telemetry)
    {
        _commands = commands;
        _telemetry = telemetry;
    }
    public void Start(string robotId)
    {
        _commands.RegisterAll(robotId);
        _telemetry.RegisterPublishers(robotId);
    }
}

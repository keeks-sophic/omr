using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Robot.Options;
using Robot.Services;

namespace Robot.Workers;

public class RobotWorker : BackgroundService
{
    private readonly ILogger<RobotWorker> _logger;
    private readonly IdentityService _identity;
    private readonly TelemetryService _telemetry;
    private readonly CommandListenerService _commands;
    private readonly NatsService _nats;
    private readonly IOptions<RobotOptions> _options;

    public RobotWorker(
        ILogger<RobotWorker> logger,
        IdentityService identity,
        TelemetryService telemetry,
        CommandListenerService commands,
        NatsService nats,
        IOptions<RobotOptions> options)
    {
        _logger = logger;
        _identity = identity;
        _telemetry = telemetry;
        _commands = commands;
        _nats = nats;
        _options = options;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var name = _options.Value.Name;

        _logger.LogInformation("Robot starting. Name: {Name}", name);

        await _nats.ConnectAsync(_options.Value.NatsUrl, stoppingToken);
        await _nats.EnsureStreamAsync("ROBOTS_TELEMETRY", $"{Robot.Topics.NatsSubjects.TelemetryPrefix}.>");
        await _nats.EnsureStreamAsync("ROBOTS_COMMAND", $"{Robot.Topics.NatsSubjects.CommandPrefix}.>");
        await _nats.EnsureStreamAsync("ROBOTS_ROUTE", $"{Robot.Topics.NatsSubjects.RoutePlanPrefix}.>", $"{Robot.Topics.NatsSubjects.RouteSegmentPrefix}.>");
        await _nats.EnsureStreamAsync("ROBOTS_CONTROL", $"{Robot.Topics.NatsSubjects.ControlPrefix}.>", $"{Robot.Topics.NatsSubjects.SyncPrefix}.>");

        await _identity.RegisterAsync(
            name,
            preferredInterface: _options.Value.Interface,
            ipOverride: _options.Value.Ip);

        var ip = _identity.GetIpAddress(
            preferredInterface: _options.Value.Interface,
            ipOverride: _options.Value.Ip);

        _telemetry.Initialize(name, ip!);
        var cmdSubjects = new[]
        {
            $"{Robot.Topics.NatsSubjects.CommandPrefix}.{ip}.>",
            $"{Robot.Topics.NatsSubjects.RoutePlanPrefix}.{ip}",
            $"{Robot.Topics.NatsSubjects.ControlPrefix}.{ip}",
            $"{Robot.Topics.NatsSubjects.SyncPrefix}.{ip}"
        };
        await _commands.StartAsync(name, ip!, cmdSubjects, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            _telemetry.TickIdle();
            _telemetry.TickBattery();
            _telemetry.TickRoute();
            // publish next segment opportunistically when moving allowed and route present
            await _telemetry.PublishStatusAsync();
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }
    }
}

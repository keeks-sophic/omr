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
        await _nats.EnsureStreamAsync(_options.Value.TelemetryStream!, _options.Value.TelemetrySubject);
        await _nats.EnsureStreamAsync(_options.Value.CommandStream!, _options.Value.CommandSubject);

        await _identity.RegisterAsync(
            name,
            preferredInterface: _options.Value.Interface,
            ipOverride: _options.Value.Ip);

        var ip = _identity.GetIpAddress(
            preferredInterface: _options.Value.Interface,
            ipOverride: _options.Value.Ip);

        _telemetry.Initialize(name, ip!);
        await _commands.StartAsync(name, ip!, _options.Value.CommandSubject, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            _telemetry.TickIdle();
            _telemetry.TickBattery();
            _telemetry.TickRoute();
            await _telemetry.PublishStatusAsync();
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }
    }
}

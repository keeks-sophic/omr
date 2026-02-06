using System.Text.Json;
using BackendV3.Infrastructure.Messaging;
using BackendV3.Messaging;
using BackendV3.Modules.Robots.Data;
using BackendV3.Modules.Robots.Messaging;

namespace BackendV3.Modules.Robots.Service;

public sealed class RobotSettingsService
{
    private readonly NatsConnection _nats;
    private readonly RobotSettingsRepository _settings;

    public RobotSettingsService(NatsConnection nats, RobotSettingsRepository settings)
    {
        _nats = nats;
        _settings = settings;
    }

    public Task<Model.RobotSettingsReportedSnapshot?> GetLatestReportedAsync(string robotId, CancellationToken ct = default) =>
        _settings.GetLatestReportedAsync(robotId, ct);

    public Task PublishDesiredAsync(string robotId, JsonDocument desiredPayload, Guid? correlationId = null)
    {
        var env = new RobotNatsEnvelope
        {
            RobotId = robotId,
            CorrelationId = correlationId,
            Type = "settings.desired",
            Payload = desiredPayload.RootElement
        };

        var data = JsonSerializer.SerializeToUtf8Bytes(env);
        var conn = _nats.Get();
        var js = conn.CreateJetStreamContext();
        js.Publish(NatsJetStreamRoutes.Subjects.SettingsDesired(robotId), data);
        return Task.CompletedTask;
    }
}


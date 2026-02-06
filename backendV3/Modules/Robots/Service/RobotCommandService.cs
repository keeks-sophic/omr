using System.Text.Json;
using BackendV3.Infrastructure.Messaging;
using BackendV3.Messaging;
using BackendV3.Modules.Robots.Data;
using BackendV3.Modules.Robots.Messaging;
using BackendV3.Modules.Robots.Model;
using NATS.Client.JetStream;

namespace BackendV3.Modules.Robots.Service;

public sealed class RobotCommandService
{
    private readonly NatsConnection _nats;
    private readonly RobotCommandRepository _commands;

    public RobotCommandService(NatsConnection nats, RobotCommandRepository commands)
    {
        _nats = nats;
        _commands = commands;
    }

    public async Task<Guid> SendCommandAsync(string robotId, string commandType, JsonDocument payload, Guid? requestedByUserId, CancellationToken ct = default)
    {
        var commandId = Guid.NewGuid();

        var outbox = new RobotCommandLog
        {
            CommandId = commandId,
            RobotId = robotId,
            CommandType = commandType,
            PayloadJson = payload.RootElement.GetRawText(),
            RequestedByUserId = requestedByUserId,
            RequestedAt = DateTimeOffset.UtcNow,
            Status = "REQUESTED"
        };
        await _commands.InsertAsync(outbox, ct);

        var env = new RobotNatsEnvelope
        {
            RobotId = robotId,
            CorrelationId = commandId,
            Type = "cmd",
            Payload = new
            {
                commandId,
                commandType,
                payload = payload.RootElement
            }
        };

        var data = JsonSerializer.SerializeToUtf8Bytes(env);
        var conn = _nats.Get();
        var js = conn.CreateJetStreamContext();
        js.Publish(NatsJetStreamRoutes.Subjects.Command(robotId), data);

        return commandId;
    }
}


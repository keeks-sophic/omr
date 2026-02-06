using System.Text.Json;
using BackendV3.Infrastructure.Messaging;
using BackendV3.Infrastructure.Persistence;
using BackendV3.Messaging;
using BackendV3.Modules.Robots.Model;
using BackendV3.Modules.Robots.Messaging;
using BackendV3.Modules.Robots.Service;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using NATS.Client;

namespace BackendV3.Modules.Robots.Worker;

public sealed class RobotsNatsIngestWorker : BackgroundService
{
    private readonly ILogger<RobotsNatsIngestWorker> _logger;
    private readonly NatsConnection _nats;
    private readonly IServiceScopeFactory _scopeFactory;

    private IConnection? _conn;

    public RobotsNatsIngestWorker(ILogger<RobotsNatsIngestWorker> logger, NatsConnection nats, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _nats = nats;
        _scopeFactory = scopeFactory;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _conn = _nats.Get();
        var js = _conn.CreateJetStreamContext();

        js.PushSubscribeAsync(
            NatsJetStreamRoutes.Subjects.IdentityAll,
            (s, a) => _ = HandleIdentityAsync(a.Message, stoppingToken),
            true,
            NATS.Client.JetStream.PushSubscribeOptions.Builder().WithDurable(NatsJetStreamRoutes.Consumers.RobotsIdentityIngest).Build());

        js.PushSubscribeAsync(
            NatsJetStreamRoutes.Subjects.CapabilityAll,
            (s, a) => _ = HandleCapabilityAsync(a.Message, stoppingToken),
            true,
            NATS.Client.JetStream.PushSubscribeOptions.Builder().WithDurable(NatsJetStreamRoutes.Consumers.RobotsCapabilityIngest).Build());

        js.PushSubscribeAsync(
            NatsJetStreamRoutes.Subjects.StatusAll,
            (s, a) => _ = HandleStatusAsync(a.Message, stoppingToken),
            true,
            NATS.Client.JetStream.PushSubscribeOptions.Builder().WithDurable(NatsJetStreamRoutes.Consumers.RobotsStatusIngest).Build());

        js.PushSubscribeAsync(
            NatsJetStreamRoutes.Subjects.TelemetryAll,
            (s, a) => _ = HandleTelemetryAsync(a.Message, stoppingToken),
            true,
            NATS.Client.JetStream.PushSubscribeOptions.Builder().WithDurable(NatsJetStreamRoutes.Consumers.RobotsTelemetryIngest).Build());

        js.PushSubscribeAsync(
            NatsJetStreamRoutes.Subjects.SettingsReportedAll,
            (s, a) => _ = HandleSettingsReportedAsync(a.Message, stoppingToken),
            true,
            NATS.Client.JetStream.PushSubscribeOptions.Builder().WithDurable(NatsJetStreamRoutes.Consumers.RobotsSettingsReportedIngest).Build());

        js.PushSubscribeAsync(
            NatsJetStreamRoutes.Subjects.CommandAckAll,
            (s, a) => _ = HandleCommandAckAsync(a.Message, stoppingToken),
            true,
            NATS.Client.JetStream.PushSubscribeOptions.Builder().WithDurable(NatsJetStreamRoutes.Consumers.RobotsCommandAckIngest).Build());

        return Task.CompletedTask;
    }

    private async Task HandleIdentityAsync(Msg msg, CancellationToken ct)
    {
        var env = TryDeserialize(msg.Data);
        if (env == null) return;
        var receivedAt = env.SentAt == default ? DateTimeOffset.UtcNow : env.SentAt;

        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hub = scope.ServiceProvider.GetRequiredService<RobotHubPublisher>();

        var robot = await db.Robots.FirstOrDefaultAsync(x => x.RobotId == env.RobotId, ct);
        if (robot == null)
        {
            robot = new Robot
            {
                RobotId = env.RobotId,
                DisplayName = env.RobotId,
                IsEnabled = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await db.Robots.AddAsync(robot, ct);
        }

        robot.LastSeenAt = receivedAt;
        robot.UpdatedAt = DateTimeOffset.UtcNow;

        var snap = new RobotIdentitySnapshot
        {
            SnapshotId = Guid.NewGuid(),
            RobotId = env.RobotId,
            Vendor = TryGetString(env.Payload, "vendor"),
            Model = TryGetString(env.Payload, "model"),
            FirmwareVersion = TryGetString(env.Payload, "firmwareVersion"),
            SerialNumber = TryGetString(env.Payload, "serialNumber"),
            PayloadJson = env.Payload.GetRawText(),
            ReceivedAt = receivedAt
        };
        await db.RobotIdentitySnapshots.AddAsync(snap, ct);
        await db.SaveChangesAsync(ct);

        await hub.RobotIdentityUpdatedAsync(env.RobotId, ct);
    }

    private async Task HandleCapabilityAsync(Msg msg, CancellationToken ct)
    {
        var env = TryDeserialize(msg.Data);
        if (env == null) return;
        var receivedAt = env.SentAt == default ? DateTimeOffset.UtcNow : env.SentAt;

        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hub = scope.ServiceProvider.GetRequiredService<RobotHubPublisher>();

        var robot = await db.Robots.FirstOrDefaultAsync(x => x.RobotId == env.RobotId, ct);
        if (robot == null)
        {
            robot = new Robot
            {
                RobotId = env.RobotId,
                DisplayName = env.RobotId,
                IsEnabled = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await db.Robots.AddAsync(robot, ct);
        }
        robot.LastSeenAt = receivedAt;
        robot.UpdatedAt = DateTimeOffset.UtcNow;

        await db.RobotCapabilitySnapshots.AddAsync(new RobotCapabilitySnapshot
        {
            SnapshotId = Guid.NewGuid(),
            RobotId = env.RobotId,
            PayloadJson = env.Payload.GetRawText(),
            ReceivedAt = receivedAt
        }, ct);
        await db.SaveChangesAsync(ct);

        await hub.RobotCapabilityUpdatedAsync(env.RobotId, ct);
    }

    private async Task HandleStatusAsync(Msg msg, CancellationToken ct)
    {
        var env = TryDeserialize(msg.Data);
        if (env == null) return;
        var receivedAt = env.SentAt == default ? DateTimeOffset.UtcNow : env.SentAt;

        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hub = scope.ServiceProvider.GetRequiredService<RobotHubPublisher>();

        var robot = await db.Robots.FirstOrDefaultAsync(x => x.RobotId == env.RobotId, ct);
        if (robot == null)
        {
            robot = new Robot
            {
                RobotId = env.RobotId,
                DisplayName = env.RobotId,
                IsEnabled = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await db.Robots.AddAsync(robot, ct);
        }
        robot.LastSeenAt = receivedAt;
        robot.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        await hub.RobotStatusUpdatedAsync(env.RobotId, env.Payload, ct);
    }

    private async Task HandleTelemetryAsync(Msg msg, CancellationToken ct)
    {
        var env = TryDeserialize(msg.Data);
        if (env == null) return;
        var receivedAt = env.SentAt == default ? DateTimeOffset.UtcNow : env.SentAt;

        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hub = scope.ServiceProvider.GetRequiredService<RobotHubPublisher>();

        var robot = await db.Robots.FirstOrDefaultAsync(x => x.RobotId == env.RobotId, ct);
        if (robot == null)
        {
            robot = new Robot
            {
                RobotId = env.RobotId,
                DisplayName = env.RobotId,
                IsEnabled = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await db.Robots.AddAsync(robot, ct);
        }
        robot.LastSeenAt = receivedAt;
        robot.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        await hub.RobotTelemetryUpdatedAsync(env.RobotId, env.Payload, ct);
    }

    private async Task HandleSettingsReportedAsync(Msg msg, CancellationToken ct)
    {
        var env = TryDeserialize(msg.Data);
        if (env == null) return;
        var receivedAt = env.SentAt == default ? DateTimeOffset.UtcNow : env.SentAt;

        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hub = scope.ServiceProvider.GetRequiredService<RobotHubPublisher>();

        var robot = await db.Robots.FirstOrDefaultAsync(x => x.RobotId == env.RobotId, ct);
        if (robot == null)
        {
            robot = new Robot
            {
                RobotId = env.RobotId,
                DisplayName = env.RobotId,
                IsEnabled = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await db.Robots.AddAsync(robot, ct);
        }
        robot.LastSeenAt = receivedAt;
        robot.UpdatedAt = DateTimeOffset.UtcNow;

        await db.RobotSettingsReportedSnapshots.AddAsync(new RobotSettingsReportedSnapshot
        {
            SnapshotId = Guid.NewGuid(),
            RobotId = env.RobotId,
            PayloadJson = env.Payload.GetRawText(),
            ReceivedAt = receivedAt
        }, ct);
        await db.SaveChangesAsync(ct);

        await hub.RobotSettingsReportedUpdatedAsync(env.RobotId, env.Payload, ct);
    }

    private async Task HandleCommandAckAsync(Msg msg, CancellationToken ct)
    {
        var env = TryDeserialize(msg.Data);
        if (env == null) return;
        var receivedAt = env.SentAt == default ? DateTimeOffset.UtcNow : env.SentAt;

        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hub = scope.ServiceProvider.GetRequiredService<RobotHubPublisher>();

        var robot = await db.Robots.FirstOrDefaultAsync(x => x.RobotId == env.RobotId, ct);
        if (robot == null)
        {
            robot = new Robot
            {
                RobotId = env.RobotId,
                DisplayName = env.RobotId,
                IsEnabled = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await db.Robots.AddAsync(robot, ct);
        }
        robot.LastSeenAt = receivedAt;
        robot.UpdatedAt = DateTimeOffset.UtcNow;

        if (env.CorrelationId.HasValue)
        {
            var cmd = await db.RobotCommandLogs.FirstOrDefaultAsync(x => x.CommandId == env.CorrelationId.Value, ct);
            if (cmd != null)
            {
                cmd.LastAckAt = receivedAt;
                cmd.Status = TryGetString(env.Payload, "status") ?? "ACKED";
            }
        }

        await db.SaveChangesAsync(ct);
        await hub.RobotCommandAckAsync(env.RobotId, env.Payload, ct);
    }

    private RobotNatsEnvelopeIn? TryDeserialize(byte[] data)
    {
        try
        {
            return JsonSerializer.Deserialize<RobotNatsEnvelopeIn>(data);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse robot message envelope");
            return null;
        }
    }

    private static string? TryGetString(JsonElement obj, string name)
    {
        if (obj.ValueKind != JsonValueKind.Object) return null;
        if (!obj.TryGetProperty(name, out var v)) return null;
        return v.ValueKind == JsonValueKind.String ? v.GetString() : v.GetRawText();
    }
}


using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BackendV2.Api.Contracts.Commands;
using BackendV2.Api.Contracts.Presence;
using BackendV2.Api.Contracts.State;
using BackendV2.Api.Contracts.Telemetry;
using BackendV2.Api.Contracts.Tasks;
using BackendV2.Api.Contracts.Routes;
using BackendV2.Api.Contracts.Logs;
using BackendV2.Api.Infrastructure.Persistence;
using BackendV2.Api.Infrastructure.Messaging;
using BackendV2.Api.Service.Ingestion;
using BackendV2.Api.Topics;
using BackendV2.Api.Contracts;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using NATS.Client;

namespace BackendV2.Api.Workers;

public class NatsConsumersWorker : BackgroundService
{
    private readonly NatsConnection _nats;
    private readonly AppDbContext _db;
    private readonly StateIngestionService _ingest;
    private readonly BackendV2.Api.Service.Config.ConfigurationService _config;
    private readonly IHubContext<BackendV2.Api.Hub.RealtimeHub> _hub;
    private readonly BackendV2.Api.Service.Traffic.TrafficControlService _traffic;
    private readonly BackendV2.Api.Service.Ingestion.TelemetryRateLimiter _rateLimiter = new BackendV2.Api.Service.Ingestion.TelemetryRateLimiter();
    private IConnection? _conn;
    private IAsyncSubscription? _hello;
    private IAsyncSubscription? _heartbeat;
    private IAsyncSubscription? _cmdAck;
    private NATS.Client.JetStream.IJetStreamPushAsyncSubscription? _stateSnapshot;
    private NATS.Client.JetStream.IJetStreamPushAsyncSubscription? _stateEvent;
    private NATS.Client.JetStream.IJetStreamPushAsyncSubscription? _battery;
    private NATS.Client.JetStream.IJetStreamPushAsyncSubscription? _health;
    private NATS.Client.JetStream.IJetStreamPushAsyncSubscription? _pose;
    private NATS.Client.JetStream.IJetStreamPushAsyncSubscription? _motion;
    private NATS.Client.JetStream.IJetStreamPushAsyncSubscription? _radar;
    private NATS.Client.JetStream.IJetStreamPushAsyncSubscription? _qr;
    private IAsyncSubscription? _taskEvent;
    private IAsyncSubscription? _routeProgress;
    private IAsyncSubscription? _logEvent;

    public NatsConsumersWorker(NatsConnection nats, AppDbContext db, StateIngestionService ingest, IHubContext<BackendV2.Api.Hub.RealtimeHub> hub, BackendV2.Api.Service.Config.ConfigurationService config, BackendV2.Api.Service.Traffic.TrafficControlService traffic)
    {
        _nats = nats;
        _db = db;
        _ingest = ingest;
        _hub = hub;
        _config = config;
        _traffic = traffic;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _conn = _nats.Get();
        _hello = _conn.SubscribeAsync("robot.*.presence.hello", async (s, a) => await HandleHelloAsync(a.Message));
        _heartbeat = _conn.SubscribeAsync("robot.*.presence.heartbeat", async (s, a) => await HandleHeartbeatAsync(a.Message));
        var js = _conn.CreateJetStreamContext();
        var cmdAckOpts = NATS.Client.JetStream.PushSubscribeOptions.Builder().WithDurable("CMDACK").Build();
        js.PushSubscribeAsync("robot.*.cmd_ack", (s, a) => HandleCmdAckAsync(a.Message), false, cmdAckOpts);
        var stateSnapOpts = NATS.Client.JetStream.PushSubscribeOptions.Builder().WithDurable("STATE_SNAPSHOT").Build();
        _stateSnapshot = js.PushSubscribeAsync("robot.*.state.snapshot", async (s, a) => await HandleStateSnapshotAsync(a.Message), false, stateSnapOpts);
        var stateEvtOpts = NATS.Client.JetStream.PushSubscribeOptions.Builder().WithDurable("STATE_EVENT").Build();
        _stateEvent = js.PushSubscribeAsync("robot.*.state.event", async (s, a) => await HandleStateEventAsync(a.Message), false, stateEvtOpts);
        var batOpts = NATS.Client.JetStream.PushSubscribeOptions.Builder().WithDurable("TEL_BAT").Build();
        _battery = js.PushSubscribeAsync("robot.*.telemetry.battery", async (s, a) => await HandleBatteryAsync(a.Message), false, batOpts);
        var healthOpts = NATS.Client.JetStream.PushSubscribeOptions.Builder().WithDurable("TEL_HEALTH").Build();
        _health = js.PushSubscribeAsync("robot.*.telemetry.health", async (s, a) => await HandleHealthAsync(a.Message), false, healthOpts);
        var poseOpts = NATS.Client.JetStream.PushSubscribeOptions.Builder().WithDurable("TEL_POSE").Build();
        _pose = js.PushSubscribeAsync("robot.*.telemetry.pose", async (s, a) => await HandlePoseAsync(a.Message), false, poseOpts);
        var motionOpts = NATS.Client.JetStream.PushSubscribeOptions.Builder().WithDurable("TEL_MOTION").Build();
        _motion = js.PushSubscribeAsync("robot.*.telemetry.motion", async (s, a) => await HandleMotionAsync(a.Message), false, motionOpts);
        var radarOpts = NATS.Client.JetStream.PushSubscribeOptions.Builder().WithDurable("TEL_RADAR").Build();
        _radar = js.PushSubscribeAsync("robot.*.telemetry.radar", async (s, a) => await HandleRadarAsync(a.Message), false, radarOpts);
        var qrOpts = NATS.Client.JetStream.PushSubscribeOptions.Builder().WithDurable("TEL_QR").Build();
        _qr = js.PushSubscribeAsync("robot.*.telemetry.qr", async (s, a) => await HandleQrAsync(a.Message), false, qrOpts);
        _taskEvent = _conn.SubscribeAsync("robot.*.task.event", async (s, a) => await HandleTaskEventAsync(a.Message));
        _routeProgress = _conn.SubscribeAsync("robot.*.route.progress", async (s, a) => await HandleRouteProgressAsync(a.Message));
        _logEvent = _conn.SubscribeAsync("robot.*.log.event", async (s, a) => await HandleLogEventAsync(a.Message));
        return Task.CompletedTask;
    }

    private async Task HandleHelloAsync(Msg msg)
    {
        var env = JsonSerializer.Deserialize<NatsEnvelope<PresenceHello>>(msg.Data);
        if (env?.Payload == null) return;
        var robot = await _db.Robots.FirstOrDefaultAsync(x => x.RobotId == env.RobotId);
        if (robot == null)
        {
            robot = new Model.Core.Robot { RobotId = env.RobotId, Connected = true, LastActive = env.Timestamp, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow };
            await _db.Robots.AddAsync(robot);
        }
        else
        {
            robot.Connected = true;
            robot.LastActive = env.Timestamp;
            robot.UpdatedAt = DateTimeOffset.UtcNow;
        }
        var session = await _db.RobotSessions.FirstOrDefaultAsync(x => x.RobotId == env.RobotId) ?? new Model.Core.RobotSession { RobotId = env.RobotId, Connected = true, LastSeen = env.Timestamp, UpdatedAt = DateTimeOffset.UtcNow };
        session.Connected = true;
        session.LastSeen = env.Timestamp;
        session.SoftwareVersion = env.Payload.SoftwareVersion;
        session.RuntimeMode = env.Payload.RuntimeMode;
        session.CapabilitiesJson = JsonSerializer.Serialize(env.Payload.Capabilities);
        session.FeatureFlagsJson = JsonSerializer.Serialize(env.Payload.FeatureFlags);
        session.UpdatedAt = DateTimeOffset.UtcNow;
        if (_db.Entry(session).State == EntityState.Detached) await _db.RobotSessions.AddAsync(session);
        await _db.SaveChangesAsync();
        await _hub.Clients.Group(BackendV2.Api.SignalR.RealtimeGroups.Robot(env.RobotId)).SendAsync(SignalRTopics.RobotPresenceHello, new { robotId = env.RobotId, uptimeMs = 0 });
        await _hub.Clients.Group(BackendV2.Api.SignalR.RealtimeGroups.Robots).SendAsync(SignalRTopics.RobotSessionUpdated, new { robotId = env.RobotId, connected = true, lastSeen = env.Timestamp, runtimeMode = session.RuntimeMode, softwareVersion = session.SoftwareVersion });
        await _config.PublishCurrentAsync(env.RobotId);
    }

    private async Task HandleHeartbeatAsync(Msg msg)
    {
        var env = JsonSerializer.Deserialize<NatsEnvelope<PresenceHeartbeat>>(msg.Data);
        if (env?.Payload == null) return;
        var session = await _db.RobotSessions.FirstOrDefaultAsync(x => x.RobotId == env.RobotId) ?? new Model.Core.RobotSession { RobotId = env.RobotId, Connected = true, LastSeen = env.Timestamp, UpdatedAt = DateTimeOffset.UtcNow };
        var wasConnected = session.Connected;
        session.Connected = true;
        session.LastSeen = env.Timestamp;
        session.UpdatedAt = DateTimeOffset.UtcNow;
        if (_db.Entry(session).State == EntityState.Detached) await _db.RobotSessions.AddAsync(session);
        await _db.SaveChangesAsync();
        await _hub.Clients.Group(BackendV2.Api.SignalR.RealtimeGroups.Robot(env.RobotId)).SendAsync(SignalRTopics.RobotPresenceHeartbeat, new { robotId = env.RobotId, uptimeMs = env.Payload.UptimeMs });
        if (!wasConnected)
        {
            await _hub.Clients.Group(BackendV2.Api.SignalR.RealtimeGroups.Robots).SendAsync(SignalRTopics.RobotSessionUpdated, new { robotId = env.RobotId, connected = true, lastSeen = env.Timestamp });
        }
    }

    private Task HandleCmdAckAsync(Msg msg)
    {
        var ack = JsonSerializer.Deserialize<CommandAck>(msg.Data);
        if (ack == null) return Task.CompletedTask;
        _ = _db.AuditEvents.AddAsync(new BackendV2.Api.Model.Ops.AuditEvent { AuditEventId = Guid.NewGuid(), Timestamp = DateTimeOffset.UtcNow, ActorUserId = null, Action = "robot.cmd_ack", TargetType = "robot", TargetId = ack.RobotId, Outcome = ack.Status ?? "UNKNOWN", DetailsJson = JsonSerializer.Serialize(new { correlationId = ack.CorrelationId, reason = ack.Reason }) });
        _ = _db.SaveChangesAsync();
        if ((ack.Status ?? "").ToUpperInvariant() == "NAK" && !string.IsNullOrWhiteSpace(ack.CorrelationId))
        {
            var outbox = _db.CommandOutbox.FirstOrDefault(x => x.CorrelationId == ack.CorrelationId);
            if (outbox != null)
            {
                outbox.LastAttempt = DateTimeOffset.UtcNow;
                if (outbox.RetryCount < 3)
                {
                    outbox.RetryCount++;
                    var env = new BackendV2.Api.Contracts.NatsEnvelope<object> { RobotId = outbox.RobotId, CorrelationId = Guid.NewGuid().ToString("N"), Timestamp = DateTimeOffset.UtcNow, Payload = JsonSerializer.Deserialize<object>(outbox.PayloadJson) ?? new { } };
                    var data = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(env);
                    var js = _conn!.CreateJetStreamContext();
                    js.Publish(outbox.Subject, data);
                    _ = _db.AuditEvents.AddAsync(new BackendV2.Api.Model.Ops.AuditEvent { AuditEventId = Guid.NewGuid(), Timestamp = DateTimeOffset.UtcNow, ActorUserId = null, Action = "robot.cmd_retry", TargetType = "robot", TargetId = outbox.RobotId, Outcome = "OK", DetailsJson = JsonSerializer.Serialize(new { originalCorrelationId = ack.CorrelationId }) });
                    _ = _db.SaveChangesAsync();
                }
                else
                {
                    outbox.Status = "DeadLetter";
                    var js = _conn!.CreateJetStreamContext();
                    var dlq = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(new { outboxId = outbox.OutboxId, robotId = outbox.RobotId, subject = outbox.Subject, payload = outbox.PayloadJson, reason = ack.Reason });
                    js.Publish("backend.deadletter", dlq);
                    _ = _db.AuditEvents.AddAsync(new BackendV2.Api.Model.Ops.AuditEvent { AuditEventId = Guid.NewGuid(), Timestamp = DateTimeOffset.UtcNow, ActorUserId = null, Action = "robot.cmd_deadletter", TargetType = "robot", TargetId = outbox.RobotId, Outcome = "DLQ", DetailsJson = JsonSerializer.Serialize(new { correlationId = ack.CorrelationId }) });
                    _ = _db.SaveChangesAsync();
                }
            }
        }
        return _hub.Clients.Group(BackendV2.Api.SignalR.RealtimeGroups.Robot(ack.RobotId))
            .SendAsync(SignalRTopics.RobotCmdAck, new { robotId = ack.RobotId, correlationId = ack.CorrelationId, status = ack.Status, reason = ack.Reason, timestamp = ack.Timestamp });
    }

    private Task HandleStateSnapshotAsync(Msg msg)
    {
        var snap = JsonSerializer.Deserialize<RobotStateSnapshot>(msg.Data);
        if (snap == null) return Task.CompletedTask;
        return _ingest.HandleStateSnapshotAsync(snap);
    }

    private Task HandleStateEventAsync(Msg msg)
    {
        var evt = JsonSerializer.Deserialize<RobotStateEvent>(msg.Data);
        if (evt == null) return Task.CompletedTask;
        return _ingest.HandleStateEventAsync(evt);
    }

    private Task HandleBatteryAsync(Msg msg)
    {
        var env = JsonSerializer.Deserialize<NatsEnvelope<BatteryTelemetry>>(msg.Data);
        if (env?.Payload == null) return Task.CompletedTask;
        _ = _db.RobotEvents.AddAsync(new BackendV2.Api.Model.Replay.RobotEvent { EventId = Guid.NewGuid(), RobotId = env.RobotId, Timestamp = DateTimeOffset.UtcNow, Type = "telemetry.battery", Payload = JsonSerializer.Serialize(env.Payload) });
        _ = _db.SaveChangesAsync();
        return _ingest.HandleBatteryAsync(env.RobotId, env.Payload);
    }

    private async Task HandleHealthAsync(Msg msg)
    {
        var env = JsonSerializer.Deserialize<NatsEnvelope<HealthTelemetry>>(msg.Data);
        if (env?.Payload == null) return;
        var payload = JsonSerializer.Serialize(env.Payload);
        await _db.RobotEvents.AddAsync(new BackendV2.Api.Model.Replay.RobotEvent { EventId = Guid.NewGuid(), RobotId = env.RobotId, Timestamp = DateTimeOffset.UtcNow, Type = "telemetry.health", Payload = payload });
        await _db.SaveChangesAsync();
        await _hub.Clients.Group(BackendV2.Api.SignalR.RealtimeGroups.Robot(env.RobotId)).SendAsync(SignalRTopics.RobotTelemetryHealth, new { robotId = env.RobotId, payload = env.Payload });
    }
    private async Task HandlePoseAsync(Msg msg)
    {
        var env = JsonSerializer.Deserialize<NatsEnvelope<PoseTelemetry>>(msg.Data);
        if (env?.Payload == null) return;
        if (!_rateLimiter.ShouldProcess($"{env.RobotId}:pose", TimeSpan.FromMilliseconds(100))) return;
        var payload = JsonSerializer.Serialize(env.Payload);
        await _db.RobotEvents.AddAsync(new BackendV2.Api.Model.Replay.RobotEvent { EventId = Guid.NewGuid(), RobotId = env.RobotId, Timestamp = DateTimeOffset.UtcNow, Type = "telemetry.pose", Payload = payload });
        await _db.SaveChangesAsync();
        await _hub.Clients.Group(BackendV2.Api.SignalR.RealtimeGroups.Robot(env.RobotId)).SendAsync(SignalRTopics.RobotTelemetryPose, new { robotId = env.RobotId, x = env.Payload.X, y = env.Payload.Y, heading = env.Payload.Heading });
    }
    private async Task HandleMotionAsync(Msg msg)
    {
        var env = JsonSerializer.Deserialize<NatsEnvelope<MotionTelemetry>>(msg.Data);
        if (env?.Payload == null) return;
        if (!_rateLimiter.ShouldProcess($"{env.RobotId}:motion", TimeSpan.FromMilliseconds(100))) return;
        var payload = JsonSerializer.Serialize(env.Payload);
        await _db.RobotEvents.AddAsync(new BackendV2.Api.Model.Replay.RobotEvent { EventId = Guid.NewGuid(), RobotId = env.RobotId, Timestamp = DateTimeOffset.UtcNow, Type = "telemetry.motion", Payload = payload });
        await _db.SaveChangesAsync();
        await _hub.Clients.Group(BackendV2.Api.SignalR.RealtimeGroups.Robot(env.RobotId)).SendAsync(SignalRTopics.RobotTelemetryMotion, new { robotId = env.RobotId, currentLinearVel = env.Payload.CurrentLinearVel, targetLinearVel = env.Payload.TargetLinearVel, motionState = env.Payload.MotionState });
        await _traffic.EmitScheduleSummaryAsync();
    }
    private async Task HandleRadarAsync(Msg msg)
    {
        var env = JsonSerializer.Deserialize<NatsEnvelope<RadarTelemetry>>(msg.Data);
        if (env?.Payload == null) return;
        var payload = JsonSerializer.Serialize(env.Payload);
        await _db.RobotEvents.AddAsync(new BackendV2.Api.Model.Replay.RobotEvent { EventId = Guid.NewGuid(), RobotId = env.RobotId, Timestamp = DateTimeOffset.UtcNow, Type = "telemetry.radar", Payload = payload });
        await _db.SaveChangesAsync();
        await _hub.Clients.Group(BackendV2.Api.SignalR.RealtimeGroups.Robot(env.RobotId)).SendAsync(SignalRTopics.RobotTelemetryRadar, new { robotId = env.RobotId, obstacleDetected = env.Payload.ObstacleDetected, distance = env.Payload.Distance });
        await _traffic.EmitScheduleSummaryAsync();
    }
    private async Task HandleQrAsync(Msg msg)
    {
        var env = JsonSerializer.Deserialize<NatsEnvelope<QrTelemetry>>(msg.Data);
        if (env?.Payload == null) return;
        var payload = JsonSerializer.Serialize(env.Payload);
        await _db.RobotEvents.AddAsync(new BackendV2.Api.Model.Replay.RobotEvent { EventId = Guid.NewGuid(), RobotId = env.RobotId, Timestamp = env.Payload.ScannedAt, Type = "telemetry.qr", Payload = payload });
        await _db.SaveChangesAsync();
        await _hub.Clients.Group(BackendV2.Api.SignalR.RealtimeGroups.Robot(env.RobotId)).SendAsync(SignalRTopics.RobotTelemetryQr, new { robotId = env.RobotId, qrCode = env.Payload.QrCode, scannedAt = env.Payload.ScannedAt });
    }
    private async Task HandleTaskEventAsync(Msg msg)
    {
        var env = JsonSerializer.Deserialize<NatsEnvelope<RobotTaskEvent>>(msg.Data);
        if (env?.Payload == null) return;
        var payload = JsonSerializer.Serialize(env.Payload);
        await _db.RobotEvents.AddAsync(new BackendV2.Api.Model.Replay.RobotEvent { EventId = Guid.NewGuid(), RobotId = env.RobotId, Timestamp = env.Payload.Timestamp, Type = "task.event", Payload = payload });
        await _db.SaveChangesAsync();
        var taskGuid = Guid.TryParse(env.Payload.TaskId, out var tg) ? tg : Guid.Empty;
        var task = taskGuid != Guid.Empty ? await _db.Tasks.FirstOrDefaultAsync(t => t.TaskId == taskGuid) : null;
        if (task != null)
        {
            task.Status = env.Payload.Status.ToUpperInvariant() switch
            {
                "STARTED" => "EXECUTING",
                "PAUSED" => "PAUSED",
                "RESUMED" => "EXECUTING",
                "CANCELLED" => "CANCELLED",
                "COMPLETED" => "COMPLETED",
                "FAILED" => "FAILED",
                _ => task.Status
            };
            task.UpdatedAt = DateTimeOffset.UtcNow;
            await _db.TaskEvents.AddAsync(new BackendV2.Api.Model.Task.TaskEvent { TaskEventId = Guid.NewGuid(), TaskId = task.TaskId, RobotId = env.RobotId, Status = env.Payload.Status, CreatedAt = DateTimeOffset.UtcNow });
            await _db.SaveChangesAsync();
        }
        var topic = env.Payload.Status.ToUpperInvariant() switch
        {
            "COMPLETED" => SignalRTopics.TaskCompleted,
            "FAILED" => SignalRTopics.TaskFailed,
            _ => SignalRTopics.TaskStatusChanged
        };
        await _hub.Clients.Group(BackendV2.Api.SignalR.RealtimeGroups.Robot(env.RobotId)).SendAsync(topic, new { robotId = env.RobotId, taskId = env.Payload.TaskId, status = env.Payload.Status, detail = env.Payload.Detail, timestamp = env.Payload.Timestamp });
    }
    private async Task HandleRouteProgressAsync(Msg msg)
    {
        var env = JsonSerializer.Deserialize<NatsEnvelope<RouteProgress>>(msg.Data);
        if (env?.Payload == null) return;
        var payload = JsonSerializer.Serialize(env.Payload);
        await _db.RobotEvents.AddAsync(new BackendV2.Api.Model.Replay.RobotEvent { EventId = Guid.NewGuid(), RobotId = env.RobotId, Timestamp = DateTimeOffset.UtcNow, Type = "route.progress", Payload = payload });
        await _db.SaveChangesAsync();
        if (Guid.TryParse(env.Payload.RouteId, out var rg))
        {
            var route = await _db.Routes.FirstOrDefaultAsync(r => r.RouteId == rg);
            if (route != null)
            {
                route.EstimatedArrivalTime = env.Payload.Eta ?? route.EstimatedArrivalTime;
                await _db.SaveChangesAsync();
            }
        }
        await _hub.Clients.Group(BackendV2.Api.SignalR.RealtimeGroups.Robot(env.RobotId)).SendAsync(SignalRTopics.RouteUpdated, new { robotId = env.RobotId, routeId = env.Payload.RouteId, segmentIndex = env.Payload.SegmentIndex, distanceAlong = env.Payload.DistanceAlong, eta = env.Payload.Eta });
    }
    private async Task HandleLogEventAsync(Msg msg)
    {
        var env = JsonSerializer.Deserialize<NatsEnvelope<RobotLogEvent>>(msg.Data);
        if (env?.Payload == null) return;
        var payload = JsonSerializer.Serialize(env.Payload);
        await _db.RobotEvents.AddAsync(new BackendV2.Api.Model.Replay.RobotEvent { EventId = Guid.NewGuid(), RobotId = env.RobotId, Timestamp = DateTimeOffset.UtcNow, Type = "log.event", Payload = payload });
        await _db.SaveChangesAsync();
        await _hub.Clients.Group(BackendV2.Api.SignalR.RealtimeGroups.Robot(env.RobotId)).SendAsync(SignalRTopics.RobotLogEvent, new { robotId = env.RobotId, level = env.Payload.Level, message = env.Payload.Message, detail = env.Payload.Detail });
    }
}

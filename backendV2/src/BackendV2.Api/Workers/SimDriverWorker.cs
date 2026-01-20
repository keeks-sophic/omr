using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BackendV2.Api.Contracts.State;
using BackendV2.Api.Contracts.Telemetry;
using BackendV2.Api.Infrastructure.Messaging;
using BackendV2.Api.Infrastructure.Persistence;
using BackendV2.Api.Model.Core;
using BackendV2.Api.Topics;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using NATS.Client.JetStream;

namespace BackendV2.Api.Workers;

public class SimDriverWorker : BackgroundService
{
    private readonly AppDbContext _db;
    private readonly NatsConnection _nats;
    private readonly IHubContext<BackendV2.Api.Hub.RealtimeHub> _hub;
    public SimDriverWorker(AppDbContext db, NatsConnection nats, IHubContext<BackendV2.Api.Hub.RealtimeHub> hub)
    {
        _db = db;
        _nats = nats;
        _hub = hub;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var rnd = new Random();
        while (!stoppingToken.IsCancellationRequested)
        {
            var sessions = await _db.SimSessions.AsNoTracking().Where(s => s.Status == "RUNNING").ToListAsync(stoppingToken);
            if (sessions.Count > 0)
            {
                var conn = _nats.Get();
                var js = conn.CreateJetStreamContext();
                foreach (var s in sessions)
                {
                    var prefix = $"SIM-{s.SimSessionId.ToString("N").Substring(0, 6)}-";
                    var robots = await _db.Robots.Where(r => r.RobotId.StartsWith(prefix)).ToListAsync(stoppingToken);
                    foreach (var r in robots)
                    {
                        var dx = (rnd.NextDouble() - 0.5) * 0.2;
                        var dy = (rnd.NextDouble() - 0.5) * 0.2;
                        var x = (r.Location?.X ?? (r.X ?? 0)) + dx;
                        var y = (r.Location?.Y ?? (r.Y ?? 0)) + dy;
                        r.Location = new NetTopologySuite.Geometries.Point(x, y) { SRID = 0 };
                        r.Battery = Math.Max((r.Battery ?? 100) - 0.01, 0);
                        r.State = "EXECUTING";
                        r.UpdatedAt = DateTimeOffset.UtcNow;
                        await _db.SaveChangesAsync(stoppingToken);
                        var pose = new PoseTelemetry { X = x, Y = y, Heading = 0 };
                        var motion = new MotionTelemetry { CurrentLinearVel = 0.5, TargetLinearVel = 0.5, MotionState = "MOVING" };
                        var poseEnv = JsonSerializer.SerializeToUtf8Bytes(new BackendV2.Api.Contracts.NatsEnvelope<PoseTelemetry> { RobotId = r.RobotId, Timestamp = DateTimeOffset.UtcNow, CorrelationId = Guid.NewGuid().ToString("N"), Payload = pose });
                        var motionEnv = JsonSerializer.SerializeToUtf8Bytes(new BackendV2.Api.Contracts.NatsEnvelope<MotionTelemetry> { RobotId = r.RobotId, Timestamp = DateTimeOffset.UtcNow, CorrelationId = Guid.NewGuid().ToString("N"), Payload = motion });
                        js.Publish(NatsTopics.RobotTelemetryPose(r.RobotId), poseEnv);
                        js.Publish(NatsTopics.RobotTelemetryMotion(r.RobotId), motionEnv);
                        var snap = new RobotStateSnapshot { RobotId = r.RobotId, Timestamp = DateTimeOffset.UtcNow, Mode = "EXECUTING", BatteryPct = r.Battery ?? 0 };
                        var snapData = JsonSerializer.SerializeToUtf8Bytes(snap);
                        js.Publish(NatsTopics.RobotStateSnapshot(r.RobotId), snapData);
                    }
                    await _hub.Clients.Group(BackendV2.Api.SignalR.RealtimeGroups.Robots).SendCoreAsync(SignalRTopics.SimMetricsUpdated, new object[] { new { simSessionId = s.SimSessionId.ToString(), robots = robots.Select(r => new { robotId = r.RobotId, battery = r.Battery, x = r.Location.X, y = r.Location.Y }) } }, CancellationToken.None);
                }
            }
            await Task.Delay(500, stoppingToken);
        }
    }
}

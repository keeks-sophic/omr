using System;
using System.Threading.Tasks;
using BackendV2.Api.Contracts.State;
using BackendV2.Api.Contracts.Telemetry;
using BackendV2.Api.Infrastructure.Persistence;
using BackendV2.Api.Model.Replay;
using BackendV2.Api.Topics;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace BackendV2.Api.Service.Ingestion;

public class StateIngestionService
{
    private readonly AppDbContext _db;
    private readonly IHubContext<BackendV2.Api.Hub.RealtimeHub> _hub;
    public StateIngestionService(AppDbContext db, IHubContext<BackendV2.Api.Hub.RealtimeHub> hub)
    {
        _db = db;
        _hub = hub;
    }

    public async Task HandleStateSnapshotAsync(RobotStateSnapshot snap)
    {
        if (!IngestionValidator.Validate(snap)) return;
        var r = await _db.Robots.FirstOrDefaultAsync(x => x.RobotId == snap.RobotId);
        if (r != null)
        {
            r.State = snap.Mode;
            r.Battery = snap.BatteryPct;
            r.LastActive = snap.Timestamp;
            r.UpdatedAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync();
            var payload = System.Text.Json.JsonSerializer.Serialize(snap);
            await _db.RobotEvents.AddAsync(new RobotEvent { EventId = Guid.NewGuid(), RobotId = snap.RobotId, Timestamp = snap.Timestamp, Type = "state.snapshot", Payload = payload });
            await _db.SaveChangesAsync();
            await _hub.Clients.Group(BackendV2.Api.SignalR.RealtimeGroups.Robot(snap.RobotId)).SendAsync(SignalRTopics.RobotStateSnapshot, new { robotId = snap.RobotId, mode = snap.Mode, batteryPct = snap.BatteryPct, timestamp = snap.Timestamp });
            await _hub.Clients.Group(BackendV2.Api.SignalR.RealtimeGroups.Robots).SendAsync(SignalRTopics.RobotStateSnapshot, new { robotId = snap.RobotId, mode = snap.Mode, batteryPct = snap.BatteryPct, timestamp = snap.Timestamp });
        }
    }

    public async Task HandleStateEventAsync(RobotStateEvent evt)
    {
        if (!IngestionValidator.Validate(evt)) return;
        await _db.RobotEvents.AddAsync(new RobotEvent { EventId = Guid.NewGuid(), RobotId = evt.RobotId, Timestamp = evt.Timestamp, Type = "state.event", Payload = evt.DetailsJson ?? "{}" });
        await _db.SaveChangesAsync();
        await _hub.Clients.Group(BackendV2.Api.SignalR.RealtimeGroups.Robot(evt.RobotId)).SendAsync(SignalRTopics.RobotStateEvent, new { robotId = evt.RobotId, eventType = evt.EventType, timestamp = evt.Timestamp, detailsJson = evt.DetailsJson });
        await _hub.Clients.Group(BackendV2.Api.SignalR.RealtimeGroups.Robots).SendAsync(SignalRTopics.RobotStateEvent, new { robotId = evt.RobotId, eventType = evt.EventType, timestamp = evt.Timestamp, detailsJson = evt.DetailsJson });
    }

    public async Task HandleBatteryAsync(string robotId, BatteryTelemetry t)
    {
        if (!IngestionValidator.Validate(t)) return;
        var r = await _db.Robots.FirstOrDefaultAsync(x => x.RobotId == robotId);
        if (r != null)
        {
            r.Battery = t.BatteryPct;
            r.UpdatedAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync();
            var payload = System.Text.Json.JsonSerializer.Serialize(t);
            await _db.RobotEvents.AddAsync(new RobotEvent { EventId = Guid.NewGuid(), RobotId = robotId, Timestamp = DateTimeOffset.UtcNow, Type = "telemetry.battery", Payload = payload });
            await _db.SaveChangesAsync();
            await _hub.Clients.Group(BackendV2.Api.SignalR.RealtimeGroups.Robot(robotId)).SendAsync(SignalRTopics.RobotTelemetryBattery, new { robotId, batteryPct = t.BatteryPct, voltage = t.Voltage });
        }
    }
}

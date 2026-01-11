using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BackendV2.Api.Infrastructure.Persistence;
using BackendV2.Api.Dto.Ops;
using BackendV2.Api.Model.Ops;
using BackendV2.Api.Topics;
using BackendV2.Api.Infrastructure.Messaging;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace BackendV2.Api.Service.Ops;

public class OpsService
{
    private readonly AppDbContext _db;
    private readonly IHubContext<BackendV2.Api.Hub.RealtimeHub> _hub;
    private readonly NatsConnection _nats;
    public OpsService(AppDbContext db, IHubContext<BackendV2.Api.Hub.RealtimeHub> hub, NatsConnection nats)
    {
        _db = db;
        _hub = hub;
        _nats = nats;
    }

    public async Task<OpsHealthDto> GetHealthAsync()
    {
        var robots = await _db.Robots.AsNoTracking().CountAsync();
        var sessions = await _db.RobotSessions.AsNoTracking().CountAsync();
        return new OpsHealthDto { Robots = robots, Sessions = sessions };
    }

    public async Task<OpsJetStreamDto> GetJetStreamAsync()
    {
        var lagEnv = Environment.GetEnvironmentVariable("BACKENDV2_JETSTREAM_LAG");
        var healthEnv = Environment.GetEnvironmentVariable("BACKENDV2_JETSTREAM_HEALTH");
        var lag = long.TryParse(lagEnv, out var l) ? l : 0;
        var connStateHealthy = false;
        try { connStateHealthy = _nats.Get().State == NATS.Client.ConnState.CONNECTED; } catch { connStateHealthy = false; }
        var consumersHealthy = connStateHealthy || string.Equals(healthEnv, "true", StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(healthEnv);
        return new OpsJetStreamDto { ConsumersHealthy = consumersHealthy, Lag = lag };
    }

    public async Task<List<OpsAlertDto>> GetAlertsAsync()
    {
        var now = DateTimeOffset.UtcNow;
        var sessions = await _db.RobotSessions.AsNoTracking().ToListAsync();
        var alerts = new List<OpsAlertDto>();
        foreach (var s in sessions)
        {
            if (!s.Connected)
            {
                alerts.Add(new OpsAlertDto { Type = "offline", Severity = "critical", RobotId = s.RobotId, Message = "Robot session offline", Timestamp = s.UpdatedAt });
            }
            else if (now - s.LastSeen > TimeSpan.FromMinutes(10))
            {
                alerts.Add(new OpsAlertDto { Type = "ingestion_stalled", Severity = "warning", RobotId = s.RobotId, Message = "No telemetry received in 10+ minutes", Timestamp = now });
            }
        }
        return alerts;
    }

    public async Task<List<AuditEvent>> GetAuditAsync(int limit = 50)
    {
        return await _db.AuditEvents.AsNoTracking().OrderByDescending(a => a.Timestamp).Take(limit).ToListAsync();
    }
}

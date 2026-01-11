using System;
using System.Threading;
using System.Threading.Tasks;
using BackendV2.Api.Infrastructure.Persistence;
using BackendV2.Api.Topics;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace BackendV2.Api.Workers;

public class OfflineDetectionWorker : BackgroundService
{
    private readonly AppDbContext _db;
    private readonly IHubContext<BackendV2.Api.Hub.RealtimeHub> _hub;
    public OfflineDetectionWorker(AppDbContext db, IHubContext<BackendV2.Api.Hub.RealtimeHub> hub)
    {
        _db = db;
        _hub = hub;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTimeOffset.UtcNow;
            var sessions = await _db.RobotSessions.ToListAsync(stoppingToken);
            foreach (var s in sessions)
            {
                if (s.Connected && now - s.LastSeen > TimeSpan.FromMinutes(5))
                {
                    s.Connected = false;
                    s.UpdatedAt = now;
                    await _hub.Clients.Group(BackendV2.Api.SignalR.RealtimeGroups.Robots).SendAsync(SignalRTopics.RobotSessionUpdated, new { robotId = s.RobotId, connected = false, lastSeen = s.LastSeen }, stoppingToken);
                    await _hub.Clients.Group(BackendV2.Api.SignalR.RealtimeGroups.Robot(s.RobotId)).SendAsync(SignalRTopics.RobotSessionUpdated, new { robotId = s.RobotId, connected = false, lastSeen = s.LastSeen }, stoppingToken);
                }
            }
            await _db.SaveChangesAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}

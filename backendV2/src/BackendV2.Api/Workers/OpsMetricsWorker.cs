using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BackendV2.Api.Dto.Ops;
using BackendV2.Api.Infrastructure.Persistence;
using BackendV2.Api.Service.Ops;
using BackendV2.Api.Topics;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace BackendV2.Api.Workers;

public class OpsMetricsWorker : BackgroundService
{
    private readonly AppDbContext _db;
    private readonly OpsService _ops;
    private readonly IHubContext<BackendV2.Api.Hub.RealtimeHub> _hub;
    private readonly HashSet<string> _offlineTracked = new();

    public OpsMetricsWorker(AppDbContext db, OpsService ops, IHubContext<BackendV2.Api.Hub.RealtimeHub> hub)
    {
        _db = db;
        _ops = ops;
        _hub = hub;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var js = await _ops.GetJetStreamAsync();
            await _hub.Clients.Group(BackendV2.Api.SignalR.RealtimeGroups.Robots)
                .SendCoreAsync(SignalRTopics.OpsJetStreamUpdated, new object[] { js }, stoppingToken);

            var sessions = await _db.RobotSessions.AsNoTracking().ToListAsync(stoppingToken);
            var now = DateTimeOffset.UtcNow;
            var currentlyOffline = sessions.Where(s => !s.Connected).Select(s => s.RobotId).ToHashSet();

            foreach (var robotId in currentlyOffline)
            {
                if (_offlineTracked.Add(robotId))
                {
                    await _hub.Clients.Group(BackendV2.Api.SignalR.RealtimeGroups.Robots)
                        .SendCoreAsync(SignalRTopics.OpsAlertRaised, new object[] { new OpsAlertDto { Type = "offline", Severity = "critical", RobotId = robotId, Message = "Robot session offline", Timestamp = now } }, stoppingToken);
                }
            }

            var cleared = _offlineTracked.Where(id => !currentlyOffline.Contains(id)).ToList();
            foreach (var robotId in cleared)
            {
                _offlineTracked.Remove(robotId);
                await _hub.Clients.Group(BackendV2.Api.SignalR.RealtimeGroups.Robots)
                    .SendCoreAsync(SignalRTopics.OpsAlertCleared, new object[] { new { type = "offline", robotId, timestamp = now } }, stoppingToken);
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}


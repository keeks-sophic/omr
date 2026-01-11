using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BackendV2.Api.Infrastructure.Persistence;
using BackendV2.Api.Model.Replay;
using BackendV2.Api.Topics;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BackendV2.Api.Service.Replay;

public class ReplayStreamingCoordinator
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _sessions = new();

    public ReplayStreamingCoordinator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Task StartAsync(Guid replaySessionId)
    {
        if (_sessions.ContainsKey(replaySessionId)) return Task.CompletedTask;
        var cts = new CancellationTokenSource();
        _sessions[replaySessionId] = cts;
        _ = Task.Run(() => StreamSessionAsync(replaySessionId, cts.Token), cts.Token);
        return Task.CompletedTask;
    }

    public Task StopAsync(Guid replaySessionId)
    {
        if (_sessions.TryRemove(replaySessionId, out var cts))
        {
            cts.Cancel();
        }
        return Task.CompletedTask;
    }

    public async Task SeekAsync(Guid replaySessionId, DateTimeOffset seekTime)
    {
        await StopAsync(replaySessionId);
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var s = await db.ReplaySessions.FirstOrDefaultAsync(x => x.ReplaySessionId == replaySessionId);
        if (s == null) return;
        s.FromTime = seekTime;
        s.Status = "RUNNING";
        await db.SaveChangesAsync();
        await StartAsync(replaySessionId);
    }

    private async Task StreamSessionAsync(Guid replaySessionId, CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hub = scope.ServiceProvider.GetRequiredService<IHubContext<BackendV2.Api.Hub.RealtimeHub>>();
        var s = await db.ReplaySessions.AsNoTracking().FirstOrDefaultAsync(x => x.ReplaySessionId == replaySessionId, ct);
        if (s == null) return;

        await hub.Clients.Group(BackendV2.Api.SignalR.RealtimeGroups.Robots)
            .SendCoreAsync(SignalRTopics.ReplaySessionStatus, new object[] { new { replaySessionId = replaySessionId.ToString(), status = "RUNNING" } }, ct);

        var events = await db.RobotEvents.AsNoTracking()
            .Where(e => e.RobotId == s.RobotId && e.Timestamp >= s.FromTime && e.Timestamp <= s.ToTime)
            .OrderBy(e => e.Timestamp)
            .ToListAsync(ct);

        DateTimeOffset? prev = null;
        foreach (var ev in events)
        {
            if (ct.IsCancellationRequested) break;
            if (prev != null)
            {
                var diff = ev.Timestamp - prev.Value;
                var delayMs = diff.TotalMilliseconds / Math.Max(0.1, s.PlaybackSpeed);
                if (delayMs > 0) await Task.Delay(TimeSpan.FromMilliseconds(delayMs), ct);
            }
            await hub.Clients.Group(BackendV2.Api.SignalR.RealtimeGroups.Robots)
                .SendCoreAsync(SignalRTopics.ReplayEvent, new object[] { new { replaySessionId = replaySessionId.ToString(), eventType = ev.Type, payload = ev.Payload, timestamp = ev.Timestamp } }, ct);
            prev = ev.Timestamp;
        }

        using var scope2 = _serviceProvider.CreateScope();
        var db2 = scope2.ServiceProvider.GetRequiredService<AppDbContext>();
        var s2 = await db2.ReplaySessions.FirstOrDefaultAsync(x => x.ReplaySessionId == replaySessionId, ct);
        if (s2 != null)
        {
            s2.Status = "COMPLETED";
            await db2.SaveChangesAsync(ct);
        }

        await hub.Clients.Group(BackendV2.Api.SignalR.RealtimeGroups.Robots)
            .SendCoreAsync(SignalRTopics.ReplaySessionStatus, new object[] { new { replaySessionId = replaySessionId.ToString(), status = "COMPLETED" } }, ct);

        _sessions.TryRemove(replaySessionId, out _);
    }
}


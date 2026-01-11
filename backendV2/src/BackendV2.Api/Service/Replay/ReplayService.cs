using System;
using System.Text.Json;
using System.Threading.Tasks;
using BackendV2.Api.Dto.Replay;
using BackendV2.Api.Infrastructure.Persistence;
using BackendV2.Api.Model.Replay;
using BackendV2.Api.Topics;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace BackendV2.Api.Service.Replay;

public class ReplayService
{
    private readonly AppDbContext _db;
    private readonly IHubContext<BackendV2.Api.Hub.RealtimeHub> _hub;
    private readonly ReplayStreamingCoordinator _coordinator;
    public ReplayService(AppDbContext db, IHubContext<BackendV2.Api.Hub.RealtimeHub> hub, ReplayStreamingCoordinator coordinator)
    {
        _db = db;
        _hub = hub;
        _coordinator = coordinator;
    }

    public async Task<ReplaySession> CreateAsync(ReplayCreateRequest req, Guid? actorUserId)
    {
        var s = new ReplaySession { ReplaySessionId = Guid.NewGuid(), RobotId = req.RobotId, FromTime = req.FromTime, ToTime = req.ToTime, PlaybackSpeed = req.PlaybackSpeed, Status = "CREATED", CreatedBy = actorUserId, CreatedAt = DateTimeOffset.UtcNow };
        await _db.ReplaySessions.AddAsync(s);
        await _db.SaveChangesAsync();
        await _hub.Clients.Group(BackendV2.Api.SignalR.RealtimeGroups.Robots).SendCoreAsync(SignalRTopics.ReplaySessionStatus, new object[] { new { replaySessionId = s.ReplaySessionId.ToString(), status = s.Status } }, System.Threading.CancellationToken.None);
        return s;
    }

    public async Task StartAsync(Guid replaySessionId)
    {
        var s = await _db.ReplaySessions.FirstOrDefaultAsync(x => x.ReplaySessionId == replaySessionId) ?? throw new InvalidOperationException("Replay session not found");
        s.Status = "RUNNING";
        await _db.SaveChangesAsync();
        await _hub.Clients.Group(BackendV2.Api.SignalR.RealtimeGroups.Robots).SendCoreAsync(SignalRTopics.ReplaySessionStatus, new object[] { new { replaySessionId = replaySessionId.ToString(), status = s.Status } }, System.Threading.CancellationToken.None);
        await _coordinator.StartAsync(replaySessionId);
    }

    public async Task StopAsync(Guid replaySessionId)
    {
        var s = await _db.ReplaySessions.FirstOrDefaultAsync(x => x.ReplaySessionId == replaySessionId) ?? throw new InvalidOperationException("Replay session not found");
        s.Status = "STOPPED";
        await _db.SaveChangesAsync();
        await _hub.Clients.Group(BackendV2.Api.SignalR.RealtimeGroups.Robots).SendCoreAsync(SignalRTopics.ReplaySessionStatus, new object[] { new { replaySessionId = replaySessionId.ToString(), status = s.Status } }, System.Threading.CancellationToken.None);
        await _coordinator.StopAsync(replaySessionId);
    }

    public async Task SeekAsync(Guid replaySessionId, ReplaySeekRequest req)
    {
        var s = await _db.ReplaySessions.AsNoTracking().FirstOrDefaultAsync(x => x.ReplaySessionId == replaySessionId) ?? throw new InvalidOperationException("Replay session not found");
        await _coordinator.SeekAsync(replaySessionId, req.SeekTime);
    }

    private async Task EmitReplayEventAsync(Guid replaySessionId, object payload)
    {
        await _hub.Clients.Group(BackendV2.Api.SignalR.RealtimeGroups.Robots).SendCoreAsync(SignalRTopics.ReplayEvent, new object[] { new { replaySessionId = replaySessionId.ToString(), payload } }, System.Threading.CancellationToken.None);
    }

    public async Task<ReplaySession?> GetAsync(Guid replaySessionId)
    {
        return await _db.ReplaySessions.AsNoTracking().FirstOrDefaultAsync(x => x.ReplaySessionId == replaySessionId);
    }
}

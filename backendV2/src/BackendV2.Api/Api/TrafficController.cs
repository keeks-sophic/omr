using System;
using System.Linq;
using System.Threading.Tasks;
using BackendV2.Api.Dto.Traffic;
using BackendV2.Api.Infrastructure.Persistence;
using BackendV2.Api.Model.Traffic;
using BackendV2.Api.Service.Traffic;
using BackendV2.Api.Topics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace BackendV2.Api.Api;

[ApiController]
[Route("api/v1/traffic")]
public class TrafficController : ControllerBase
{
    [HttpGet("health")]
    public IActionResult Health() => Ok(new { ok = true });

    [Authorize]
    [HttpGet("conflicts")]
    public async Task<IActionResult> GetConflicts([FromServices] TrafficControlService traffic)
    {
        var conflicts = await traffic.GetConflictsAsync();
        return Ok(conflicts);
    }

    public class TrafficHoldRequest
    {
        public Guid MapVersionId { get; set; }
        public Guid? NodeId { get; set; }
        public Guid? PathId { get; set; }
        public string Reason { get; set; } = "manual";
        public int DurationSeconds { get; set; } = 60;
    }

    [Authorize]
    [HttpPost("holds")]
    public async Task<IActionResult> CreateHold([FromBody] TrafficHoldRequest request, [FromServices] AppDbContext db, [FromServices] IHubContext<BackendV2.Api.Hub.RealtimeHub> hub)
    {
        var hold = new TrafficHold
        {
            HoldId = Guid.NewGuid(),
            MapVersionId = request.MapVersionId,
            NodeId = request.NodeId,
            PathId = request.PathId,
            Reason = request.Reason,
            StartTime = DateTimeOffset.UtcNow,
            EndTime = DateTimeOffset.UtcNow.AddSeconds(request.DurationSeconds),
            CreatedBy = null,
            CreatedAt = DateTimeOffset.UtcNow
        };
        await db.TrafficHolds.AddAsync(hold);
        await db.SaveChangesAsync();
        await hub.Clients.Group(BackendV2.Api.SignalR.RealtimeGroups.Robots).SendCoreAsync(SignalRTopics.TrafficHoldCreated, new object[] { new { holdId = hold.HoldId.ToString(), mapVersionId = hold.MapVersionId.ToString(), nodeId = hold.NodeId?.ToString(), pathId = hold.PathId?.ToString(), reason = hold.Reason } }, System.Threading.CancellationToken.None);
        await hub.Clients.Group(BackendV2.Api.SignalR.RealtimeGroups.Robots).SendCoreAsync(SignalRTopics.TrafficOverviewUpdated, new object[] { new { holds = 1 } }, System.Threading.CancellationToken.None);
        return Ok(new { holdId = hold.HoldId });
    }

    [Authorize]
    [HttpDelete("holds/{holdId}")]
    public async Task<IActionResult> DeleteHold(Guid holdId, [FromServices] AppDbContext db, [FromServices] IHubContext<BackendV2.Api.Hub.RealtimeHub> hub)
    {
        var hold = await db.TrafficHolds.FirstOrDefaultAsync(x => x.HoldId == holdId);
        if (hold == null) return NotFound();
        db.TrafficHolds.Remove(hold);
        await db.SaveChangesAsync();
        await hub.Clients.Group(BackendV2.Api.SignalR.RealtimeGroups.Robots).SendCoreAsync(SignalRTopics.TrafficHoldReleased, new object[] { new { holdId = holdId.ToString() } }, System.Threading.CancellationToken.None);
        await hub.Clients.Group(BackendV2.Api.SignalR.RealtimeGroups.Robots).SendCoreAsync(SignalRTopics.TrafficOverviewUpdated, new object[] { new { holds = -1 } }, System.Threading.CancellationToken.None);
        return Ok(new { ok = true });
    }

    [Authorize]
    [HttpGet("holds")]
    public async Task<IActionResult> ListHolds([FromServices] AppDbContext db)
    {
        var holds = await db.TrafficHolds.AsNoTracking().Select(h => new { holdId = h.HoldId, mapVersionId = h.MapVersionId, nodeId = h.NodeId, pathId = h.PathId, reason = h.Reason, startTime = h.StartTime, endTime = h.EndTime }).ToListAsync();
        return Ok(holds);
    }

    [Authorize]
    [HttpGet("robots/{robotId}/schedule")]
    public async Task<IActionResult> GetRobotSchedule(string robotId, [FromServices] TrafficControlService traffic)
    {
        var summaries = await traffic.ComputeScheduleSummariesAsync();
        var s = summaries.FirstOrDefault(x => x.RobotId == robotId);
        if (s == null) return NotFound();
        var now = DateTimeOffset.UtcNow;
        var points = new System.Collections.Generic.List<BackendV2.Api.Contracts.Traffic.SchedulePoint>
        {
            new BackendV2.Api.Contracts.Traffic.SchedulePoint { TMs = 0, TargetVel = s.TargetLinearVel },
            new BackendV2.Api.Contracts.Traffic.SchedulePoint { TMs = 500, TargetVel = s.TargetLinearVel },
            new BackendV2.Api.Contracts.Traffic.SchedulePoint { TMs = 1500, TargetVel = s.TargetLinearVel }
        };
        var schedule = new BackendV2.Api.Contracts.Traffic.TrafficSchedule { ScheduleId = Guid.NewGuid().ToString("N"), GeneratedAt = now, HorizonMs = 2000, Points = points.ToArray() };
        return Ok(schedule);
    }
}

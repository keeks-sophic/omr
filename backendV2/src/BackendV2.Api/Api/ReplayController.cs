using System;
using System.Threading.Tasks;
using BackendV2.Api.Dto.Replay;
using BackendV2.Api.Service.Replay;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackendV2.Api.Api;

[ApiController]
[Route("api/v1/replay/sessions")]
public class ReplayController : ControllerBase
{
[Authorize(Policy = BackendV2.Api.Infrastructure.Security.AuthorizationPolicies.Operator)]
[HttpPost]
public async Task<IActionResult> Create([FromBody] ReplayCreateRequest req, [FromServices] ReplayService replay)
{
        var actor = User.FindFirst("sub")?.Value;
        var s = await replay.CreateAsync(req, Guid.TryParse(actor, out var g) ? g : null);
        await using (var db = HttpContext.RequestServices.GetRequiredService<BackendV2.Api.Infrastructure.Persistence.AppDbContext>())
        {
            System.Guid? actorId = System.Guid.TryParse(actor, out var g2) ? g2 : null;
            await db.AuditEvents.AddAsync(new BackendV2.Api.Model.Ops.AuditEvent { AuditEventId = System.Guid.NewGuid(), Timestamp = System.DateTimeOffset.UtcNow, ActorUserId = actorId, Action = "replay.create", TargetType = "replay", TargetId = s.ReplaySessionId.ToString(), Outcome = "OK", DetailsJson = "{}" });
            await db.SaveChangesAsync();
        }
        return Ok(new { replaySessionId = s.ReplaySessionId });
    }

[Authorize(Policy = BackendV2.Api.Infrastructure.Security.AuthorizationPolicies.Operator)]
[HttpPost("{replaySessionId}/start")]
public async Task<IActionResult> Start(Guid replaySessionId, [FromServices] ReplayService replay)
{
    await replay.StartAsync(replaySessionId);
    await using (var db = HttpContext.RequestServices.GetRequiredService<BackendV2.Api.Infrastructure.Persistence.AppDbContext>())
    {
        var actor = User.FindFirst("sub")?.Value;
        System.Guid? actorId = System.Guid.TryParse(actor, out var g2) ? g2 : null;
        await db.AuditEvents.AddAsync(new BackendV2.Api.Model.Ops.AuditEvent { AuditEventId = System.Guid.NewGuid(), Timestamp = System.DateTimeOffset.UtcNow, ActorUserId = actorId, Action = "replay.start", TargetType = "replay", TargetId = replaySessionId.ToString(), Outcome = "OK", DetailsJson = "{}" });
        await db.SaveChangesAsync();
    }
    return Ok(new { ok = true });
}

[Authorize(Policy = BackendV2.Api.Infrastructure.Security.AuthorizationPolicies.Operator)]
[HttpPost("{replaySessionId}/stop")]
public async Task<IActionResult> Stop(Guid replaySessionId, [FromServices] ReplayService replay)
{
    await replay.StopAsync(replaySessionId);
    await using (var db = HttpContext.RequestServices.GetRequiredService<BackendV2.Api.Infrastructure.Persistence.AppDbContext>())
    {
        var actor = User.FindFirst("sub")?.Value;
        System.Guid? actorId = System.Guid.TryParse(actor, out var g2) ? g2 : null;
        await db.AuditEvents.AddAsync(new BackendV2.Api.Model.Ops.AuditEvent { AuditEventId = System.Guid.NewGuid(), Timestamp = System.DateTimeOffset.UtcNow, ActorUserId = actorId, Action = "replay.stop", TargetType = "replay", TargetId = replaySessionId.ToString(), Outcome = "OK", DetailsJson = "{}" });
        await db.SaveChangesAsync();
    }
    return Ok(new { ok = true });
}

[Authorize(Policy = BackendV2.Api.Infrastructure.Security.AuthorizationPolicies.Operator)]
[HttpPost("{replaySessionId}/seek")]
public async Task<IActionResult> Seek(Guid replaySessionId, [FromBody] ReplaySeekRequest req, [FromServices] ReplayService replay)
{
    await replay.SeekAsync(replaySessionId, req);
    await using (var db = HttpContext.RequestServices.GetRequiredService<BackendV2.Api.Infrastructure.Persistence.AppDbContext>())
    {
        var actor = User.FindFirst("sub")?.Value;
        System.Guid? actorId = System.Guid.TryParse(actor, out var g2) ? g2 : null;
        await db.AuditEvents.AddAsync(new BackendV2.Api.Model.Ops.AuditEvent { AuditEventId = System.Guid.NewGuid(), Timestamp = System.DateTimeOffset.UtcNow, ActorUserId = actorId, Action = "replay.seek", TargetType = "replay", TargetId = replaySessionId.ToString(), Outcome = "OK", DetailsJson = "{}" });
        await db.SaveChangesAsync();
    }
    return Ok(new { ok = true });
}

[Authorize(Policy = BackendV2.Api.Infrastructure.Security.AuthorizationPolicies.Operator)]
[HttpGet("{replaySessionId}")]
public async Task<IActionResult> Get(Guid replaySessionId, [FromServices] ReplayService replay)
{
        var s = await replay.GetAsync(replaySessionId);
        if (s == null) return NotFound(new { message = "Replay session not found" });
        return Ok(new { replaySessionId = s.ReplaySessionId, robotId = s.RobotId, fromTime = s.FromTime, toTime = s.ToTime, playbackSpeed = s.PlaybackSpeed, status = s.Status, createdBy = s.CreatedBy, createdAt = s.CreatedAt });
    }
}

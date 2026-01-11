using System;
using System.Threading.Tasks;
using BackendV2.Api.Dto.Teach;
using BackendV2.Api.Service.Teach;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackendV2.Api.Api;

[ApiController]
[Route("api/v1/teach")]
public class TeachController : ControllerBase
{
[Authorize(Policy = BackendV2.Api.Infrastructure.Security.AuthorizationPolicies.Planner)]
[HttpPost("sessions")]
public async Task<IActionResult> CreateSession([FromBody] TeachSessionCreateRequest req, [FromServices] TeachingService teach)
{
    var actor = User.FindFirst("sub")?.Value;
    var s = await teach.CreateSessionAsync(req, Guid.TryParse(actor, out var g) ? g : null);
    await using (var db = HttpContext.RequestServices.GetRequiredService<BackendV2.Api.Infrastructure.Persistence.AppDbContext>())
    {
        System.Guid? actorId = System.Guid.TryParse(actor, out var g2) ? g2 : null;
        await db.AuditEvents.AddAsync(new BackendV2.Api.Model.Ops.AuditEvent { AuditEventId = System.Guid.NewGuid(), Timestamp = System.DateTimeOffset.UtcNow, ActorUserId = actorId, Action = "teach.session.create", TargetType = "teach", TargetId = s.TeachSessionId.ToString(), Outcome = "OK", DetailsJson = "{}" });
        await db.SaveChangesAsync();
    }
    return Ok(new { teachSessionId = s.TeachSessionId });
}

[Authorize(Policy = BackendV2.Api.Infrastructure.Security.AuthorizationPolicies.Planner)]
[HttpPost("sessions/{teachSessionId}/start")]
public async Task<IActionResult> Start(Guid teachSessionId, [FromServices] TeachingService teach)
{
    await teach.StartSessionAsync(teachSessionId);
    await using (var db = HttpContext.RequestServices.GetRequiredService<BackendV2.Api.Infrastructure.Persistence.AppDbContext>())
    {
        var actor = User.FindFirst("sub")?.Value;
        System.Guid? actorId = System.Guid.TryParse(actor, out var g2) ? g2 : null;
        await db.AuditEvents.AddAsync(new BackendV2.Api.Model.Ops.AuditEvent { AuditEventId = System.Guid.NewGuid(), Timestamp = System.DateTimeOffset.UtcNow, ActorUserId = actorId, Action = "teach.session.start", TargetType = "teach", TargetId = teachSessionId.ToString(), Outcome = "OK", DetailsJson = "{}" });
        await db.SaveChangesAsync();
    }
    return Ok(new { ok = true });
}

[Authorize(Policy = BackendV2.Api.Infrastructure.Security.AuthorizationPolicies.Planner)]
[HttpPost("sessions/{teachSessionId}/stop")]
public async Task<IActionResult> Stop(Guid teachSessionId, [FromServices] TeachingService teach)
{
    await teach.StopSessionAsync(teachSessionId);
    await using (var db = HttpContext.RequestServices.GetRequiredService<BackendV2.Api.Infrastructure.Persistence.AppDbContext>())
    {
        var actor = User.FindFirst("sub")?.Value;
        System.Guid? actorId = System.Guid.TryParse(actor, out var g2) ? g2 : null;
        await db.AuditEvents.AddAsync(new BackendV2.Api.Model.Ops.AuditEvent { AuditEventId = System.Guid.NewGuid(), Timestamp = System.DateTimeOffset.UtcNow, ActorUserId = actorId, Action = "teach.session.stop", TargetType = "teach", TargetId = teachSessionId.ToString(), Outcome = "OK", DetailsJson = "{}" });
        await db.SaveChangesAsync();
    }
    return Ok(new { ok = true });
}

[Authorize(Policy = BackendV2.Api.Infrastructure.Security.AuthorizationPolicies.Planner)]
[HttpPost("sessions/{teachSessionId}/capture-step")]
public async Task<IActionResult> Capture(Guid teachSessionId, [FromBody] TeachCaptureRequest req, [FromServices] TeachingService teach)
{
    await teach.CaptureStepAsync(teachSessionId, req);
    await using (var db = HttpContext.RequestServices.GetRequiredService<BackendV2.Api.Infrastructure.Persistence.AppDbContext>())
    {
        var actor = User.FindFirst("sub")?.Value;
        System.Guid? actorId = System.Guid.TryParse(actor, out var g2) ? g2 : null;
        await db.AuditEvents.AddAsync(new BackendV2.Api.Model.Ops.AuditEvent { AuditEventId = System.Guid.NewGuid(), Timestamp = System.DateTimeOffset.UtcNow, ActorUserId = actorId, Action = "teach.step.capture", TargetType = "teach", TargetId = teachSessionId.ToString(), Outcome = "OK", DetailsJson = "{}" });
        await db.SaveChangesAsync();
    }
    return Ok(new { ok = true });
}

    public class SaveMissionRequest { public string Name { get; set; } = string.Empty; }

[Authorize(Policy = BackendV2.Api.Infrastructure.Security.AuthorizationPolicies.Planner)]
[HttpPost("sessions/{teachSessionId}/save-mission")]
public async Task<IActionResult> SaveMission(Guid teachSessionId, [FromBody] SaveMissionRequest req, [FromServices] TeachingService teach)
{
    var m = await teach.SaveMissionAsync(teachSessionId, req.Name);
    await using (var db = HttpContext.RequestServices.GetRequiredService<BackendV2.Api.Infrastructure.Persistence.AppDbContext>())
    {
        var actor = User.FindFirst("sub")?.Value;
        System.Guid? actorId = System.Guid.TryParse(actor, out var g2) ? g2 : null;
        await db.AuditEvents.AddAsync(new BackendV2.Api.Model.Ops.AuditEvent { AuditEventId = System.Guid.NewGuid(), Timestamp = System.DateTimeOffset.UtcNow, ActorUserId = actorId, Action = "teach.mission.save", TargetType = "teach", TargetId = m.MissionId.ToString(), Outcome = "OK", DetailsJson = "{}" });
        await db.SaveChangesAsync();
    }
    return Ok(new { missionId = m.MissionId });
}

[Authorize]
[HttpGet("sessions/{teachSessionId}")]
public async Task<IActionResult> GetSession(Guid teachSessionId, [FromServices] BackendV2.Api.Infrastructure.Persistence.AppDbContext db)
{
    var s = await db.TeachSessions.AsNoTracking().FirstOrDefaultAsync(x => x.TeachSessionId == teachSessionId);
    if (s == null) return NotFound();
    var steps = string.IsNullOrWhiteSpace(s.CapturedStepsJson) ? new object[] { } : System.Text.Json.JsonSerializer.Deserialize<object[]>(s.CapturedStepsJson) ?? new object[] { };
    return Ok(new { teachSessionId = s.TeachSessionId, robotId = s.RobotId, mapVersionId = s.MapVersionId, status = s.Status, createdAt = s.CreatedAt, startedAt = s.StartedAt, stoppedAt = s.StoppedAt, steps });
}
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using BackendV2.Api.Infrastructure.Persistence;

namespace BackendV2.Api.Api;

[ApiController]
[Route("api/v1/fleet")]
public class FleetController : ControllerBase
{
    [HttpGet("health")]
    public IActionResult Health() => Ok(new { ok = true });

    [Authorize]
    [HttpGet("incidents")]
    public async System.Threading.Tasks.Task<IActionResult> ListIncidents([FromServices] AppDbContext db)
    {
        var incidents = await db.RobotEvents.AsNoTracking().OrderByDescending(x => x.Timestamp).Take(100).Select(x => new { eventId = x.EventId, robotId = x.RobotId, type = x.Type, timestamp = x.Timestamp, payload = x.Payload }).ToListAsync();
        return Ok(incidents);
    }

    [Authorize(Policy = BackendV2.Api.Infrastructure.Security.AuthorizationPolicies.Operator)]
    [HttpPost("incidents/{eventId}/ack")]
    public async System.Threading.Tasks.Task<IActionResult> AckIncident(System.Guid eventId, [FromServices] AppDbContext db)
    {
        var evt = await db.RobotEvents.FirstOrDefaultAsync(x => x.EventId == eventId);
        if (evt == null) return NotFound();
        await db.AuditEvents.AddAsync(new BackendV2.Api.Model.Ops.AuditEvent { AuditEventId = System.Guid.NewGuid(), Timestamp = System.DateTimeOffset.UtcNow, ActorUserId = System.Guid.TryParse(User.FindFirst("sub")?.Value, out var g) ? g : null, Action = "fleet.incident.ack", TargetType = "robot_event", TargetId = eventId.ToString(), Outcome = "OK", DetailsJson = "{}" });
        await db.SaveChangesAsync();
        return Ok(new { ok = true });
    }
}

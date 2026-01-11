using System;
using System.Linq;
using System.Threading.Tasks;
using BackendV2.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BackendV2.Api.Api;

[ApiController]
[Route("api/v1/replay/events")]
public class ReplayEventsController : ControllerBase
{
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string robotId, [FromQuery] string? type, [FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to, [FromQuery] int? limit, [FromServices] AppDbContext db)
    {
        var q = db.RobotEvents.AsNoTracking().Where(e => e.RobotId == robotId);
        if (!string.IsNullOrWhiteSpace(type)) q = q.Where(e => e.Type == type || e.Type.StartsWith(type));
        if (from != null) q = q.Where(e => e.Timestamp >= from);
        if (to != null) q = q.Where(e => e.Timestamp <= to);
        var list = await q.OrderByDescending(e => e.Timestamp).Take(limit ?? 500).Select(e => new { timestamp = e.Timestamp, type = e.Type, payload = e.Payload }).ToListAsync();
        return Ok(list);
    }
}

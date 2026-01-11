using System;
using System.Linq;
using System.Threading.Tasks;
using BackendV2.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BackendV2.Api.Api;

[ApiController]
[Route("api/v1/robots")]
public class RobotsController : ControllerBase
{
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> List([FromServices] AppDbContext db)
    {
        var robots = await db.Robots.AsNoTracking().Select(r => new { robotId = r.RobotId, name = r.Name, batteryPct = r.BatteryPct, mode = r.Mode }).ToListAsync();
        return Ok(robots);
    }

    [Authorize]
    [HttpGet("{robotId}")]
    public async Task<IActionResult> Get(Guid robotId, [FromServices] AppDbContext db)
    {
        var r = await db.Robots.AsNoTracking().FirstOrDefaultAsync(x => x.RobotId == robotId);
        if (r == null) return NotFound();
        return Ok(new { robotId = r.RobotId, name = r.Name, batteryPct = r.BatteryPct, mode = r.Mode, x = r.Location?.X ?? r.X, y = r.Location?.Y ?? r.Y });
    }

    [Authorize]
    [HttpGet("{robotId}/session")]
    public async Task<IActionResult> Session(Guid robotId, [FromServices] AppDbContext db)
    {
        var s = await db.RobotSessions.AsNoTracking().FirstOrDefaultAsync(x => x.RobotId == robotId);
        if (s == null) return NotFound();
        return Ok(new { robotId = s.RobotId, connected = s.Connected, lastSeen = s.LastSeen, runtimeMode = s.RuntimeMode, softwareVersion = s.SoftwareVersion, capabilitiesJson = s.CapabilitiesJson, featureFlagsJson = s.FeatureFlagsJson });
    }

    [Authorize]
    [HttpGet("{robotId}/capabilities")]
    public async Task<IActionResult> Capabilities(Guid robotId, [FromServices] AppDbContext db)
    {
        var s = await db.RobotSessions.AsNoTracking().FirstOrDefaultAsync(x => x.RobotId == robotId);
        if (s == null) return NotFound();
        return Ok(new { robotId = s.RobotId, capabilities = string.IsNullOrWhiteSpace(s.CapabilitiesJson) ? new { } : System.Text.Json.JsonSerializer.Deserialize<object>(s.CapabilitiesJson) });
    }

    [Authorize]
    [HttpGet("{robotId}/feature-flags")]
    public async Task<IActionResult> FeatureFlags(Guid robotId, [FromServices] AppDbContext db)
    {
        var s = await db.RobotSessions.AsNoTracking().FirstOrDefaultAsync(x => x.RobotId == robotId);
        if (s == null) return NotFound();
        return Ok(new { robotId = s.RobotId, featureFlags = string.IsNullOrWhiteSpace(s.FeatureFlagsJson) ? new { } : System.Text.Json.JsonSerializer.Deserialize<object>(s.FeatureFlagsJson) });
    }

    [Authorize]
    [HttpGet("{robotId}/history/state")]
    public async Task<IActionResult> StateHistory(Guid robotId, [FromServices] AppDbContext db)
    {
        var states = await db.RobotEvents.AsNoTracking().Where(e => e.RobotId == robotId && e.Type == "state").OrderByDescending(e => e.Timestamp).Take(200).Select(e => new { timestamp = e.Timestamp, payload = e.Payload }).ToListAsync();
        return Ok(states);
    }

    [Authorize]
    [HttpGet("{robotId}/history/telemetry")]
    public async Task<IActionResult> TelemetryHistory(Guid robotId, [FromServices] AppDbContext db)
    {
        var telem = await db.RobotEvents.AsNoTracking().Where(e => e.RobotId == robotId && e.Type == "telemetry").OrderByDescending(e => e.Timestamp).Take(200).Select(e => new { timestamp = e.Timestamp, payload = e.Payload }).ToListAsync();
        return Ok(telem);
    }

    [Authorize]
    [HttpGet("{robotId}/history/logs")]
    public async Task<IActionResult> LogHistory(Guid robotId, [FromServices] AppDbContext db)
    {
        var logs = await db.RobotEvents.AsNoTracking().Where(e => e.RobotId == robotId && e.Type == "log").OrderByDescending(e => e.Timestamp).Take(200).Select(e => new { timestamp = e.Timestamp, payload = e.Payload }).ToListAsync();
        return Ok(logs);
    }
}

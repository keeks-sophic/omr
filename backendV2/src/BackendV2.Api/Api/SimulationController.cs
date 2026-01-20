using System;
using System.Threading.Tasks;
using BackendV2.Api.Dto.Sim;
using BackendV2.Api.Service.Sim;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BackendV2.Api.Api;

[ApiController]
[Route("api/v1/sim/sessions")]
public class SimulationController : ControllerBase
{
    [Authorize(Policy = BackendV2.Api.Infrastructure.Security.AuthorizationPolicies.Admin)]
    [HttpGet("{simSessionId}")]
    public async Task<IActionResult> Get(Guid simSessionId, [FromServices] BackendV2.Api.Infrastructure.Persistence.AppDbContext db)
    {
        var s = await db.SimSessions.AsNoTracking().FirstOrDefaultAsync(x => x.SimSessionId == simSessionId);
        if (s == null) return NotFound();
        var robots = await db.Robots.AsNoTracking().CountAsync(r => r.MapVersionId == s.MapVersionId && r.RobotId.StartsWith($"SIM-{s.SimSessionId.ToString("N").Substring(0, 6)}-"));
        return Ok(new { simSessionId = s.SimSessionId, mapVersionId = s.MapVersionId, status = s.Status, speedMultiplier = s.SpeedMultiplier, createdAt = s.CreatedAt, updatedAt = s.UpdatedAt, robots });
    }

    [Authorize(Policy = BackendV2.Api.Infrastructure.Security.AuthorizationPolicies.Admin)]
    [HttpGet("{simSessionId}/metrics")]
    public async Task<IActionResult> Metrics(Guid simSessionId, [FromServices] BackendV2.Api.Infrastructure.Persistence.AppDbContext db)
    {
        var s = await db.SimSessions.AsNoTracking().FirstOrDefaultAsync(x => x.SimSessionId == simSessionId);
        if (s == null) return NotFound();
        var prefix = $"SIM-{s.SimSessionId.ToString("N").Substring(0, 6)}-";
        var robots = await db.Robots.AsNoTracking().Where(r => r.RobotId.StartsWith(prefix)).Select(r => new { robotId = r.RobotId, battery = r.Battery, state = r.State, x = r.Location != null ? r.Location.X : (r.X ?? 0), y = r.Location != null ? r.Location.Y : (r.Y ?? 0) }).ToListAsync();
        return Ok(new { simSessionId = s.SimSessionId, robots });
    }
[Authorize(Policy = BackendV2.Api.Infrastructure.Security.AuthorizationPolicies.Admin)]
[HttpPost]
public async Task<IActionResult> Create([FromBody] SimSessionCreateRequest req, [FromServices] SimulationService sim)
{
        var actor = User.FindFirst("sub")?.Value;
        var s = await sim.CreateAsync(req, Guid.TryParse(actor, out var g) ? g : null);
        return Ok(new { simSessionId = s.SimSessionId });
    }

[Authorize(Policy = BackendV2.Api.Infrastructure.Security.AuthorizationPolicies.Admin)]
[HttpPost("{simSessionId}/start")]
public async Task<IActionResult> Start(Guid simSessionId, [FromServices] SimulationService sim)
{
        await sim.StartAsync(simSessionId);
        return Ok(new { ok = true });
    }

[Authorize(Policy = BackendV2.Api.Infrastructure.Security.AuthorizationPolicies.Admin)]
[HttpPost("{simSessionId}/stop")]
public async Task<IActionResult> Stop(Guid simSessionId, [FromServices] SimulationService sim)
{
        await sim.StopAsync(simSessionId);
        return Ok(new { ok = true });
    }

[Authorize(Policy = BackendV2.Api.Infrastructure.Security.AuthorizationPolicies.Admin)]
[HttpPost("{simSessionId}/pause")]
public async Task<IActionResult> Pause(Guid simSessionId, [FromServices] SimulationService sim)
{
        await sim.PauseAsync(simSessionId);
        return Ok(new { ok = true });
    }

[Authorize(Policy = BackendV2.Api.Infrastructure.Security.AuthorizationPolicies.Admin)]
[HttpPost("{simSessionId}/resume")]
public async Task<IActionResult> Resume(Guid simSessionId, [FromServices] SimulationService sim)
{
        await sim.ResumeAsync(simSessionId);
        return Ok(new { ok = true });
    }
}

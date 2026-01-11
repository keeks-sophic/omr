using System;
using System.Threading.Tasks;
using BackendV2.Api.Dto.Missions;
using BackendV2.Api.Service.Missions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackendV2.Api.Api;

[ApiController]
[Route("api/v1/missions")]
public class MissionsController : ControllerBase
{
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] MissionCreateRequest req, [FromServices] MissionService missions)
    {
        var actor = User.FindFirst("sub")?.Value;
        var m = await missions.CreateAsync(req, Guid.TryParse(actor, out var g) ? g : null);
        return Ok(new { missionId = m.MissionId });
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> List([FromServices] BackendV2.Api.Infrastructure.Persistence.AppDbContext db)
    {
        var list = await db.Missions.AsNoTracking().Select(m => new { missionId = m.MissionId, name = m.Name, version = m.Version, createdAt = m.CreatedAt }).ToListAsync();
        return Ok(list);
    }

    [Authorize]
    [HttpPut("{missionId}")]
    public async Task<IActionResult> Update(Guid missionId, [FromBody] MissionUpdateRequest req, [FromServices] MissionService missions)
    {
        var m = await missions.UpdateAsync(missionId, req);
        return Ok(new { missionId = m.MissionId });
    }

    [Authorize]
    [HttpGet("{missionId}")]
    public async Task<IActionResult> Get(Guid missionId, [FromServices] BackendV2.Api.Infrastructure.Persistence.AppDbContext db)
    {
        var m = await db.Missions.AsNoTracking().FirstOrDefaultAsync(x => x.MissionId == missionId);
        if (m == null) return NotFound();
        var steps = string.IsNullOrWhiteSpace(m.StepsJson) ? new object[] { } : System.Text.Json.JsonSerializer.Deserialize<object[]>(m.StepsJson) ?? new object[] { };
        return Ok(new { missionId = m.MissionId, name = m.Name, version = m.Version, createdAt = m.CreatedAt, steps });
    }

    [Authorize]
    [HttpPost("{missionId}/clone")]
    public async Task<IActionResult> Clone(Guid missionId, [FromServices] BackendV2.Api.Infrastructure.Persistence.AppDbContext db, [FromServices] MissionService missions)
    {
        var src = await db.Missions.AsNoTracking().FirstOrDefaultAsync(x => x.MissionId == missionId);
        if (src == null) return NotFound();
        var req = new MissionCreateRequest { Name = src.Name + " Copy", Steps = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.List<BackendV2.Api.Dto.Missions.MissionStepDto>>(src.StepsJson) ?? new System.Collections.Generic.List<BackendV2.Api.Dto.Missions.MissionStepDto>() };
        var actor = User.FindFirst("sub")?.Value;
        var m = await missions.CreateAsync(req, Guid.TryParse(actor, out var g) ? g : null);
        return Ok(new { missionId = m.MissionId });
    }
    public class MissionValidateRequest { public string RobotId { get; set; } = string.Empty; }

    [Authorize]
    [HttpPost("{missionId}/validate")]
    public async Task<IActionResult> Validate(Guid missionId, [FromBody] MissionValidateRequest req, [FromServices] MissionService missions)
    {
        var ok = await missions.ValidateAsync(missionId, req.RobotId);
        return Ok(new { valid = ok });
    }
}

using System;
using System.Threading.Tasks;
using BackendV2.Api.Dto.Tasks;
using BackendV2.Api.Service.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BackendV2.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BackendV2.Api.Api;

[ApiController]
[Route("api/v1/tasks")]
public class TasksController : ControllerBase
{
    [HttpGet("health")]
    public IActionResult Health() => Ok(new { ok = true });

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> List([FromServices] AppDbContext db)
    {
        var list = await db.Tasks.AsNoTracking().Select(t => new { taskId = t.TaskId, status = t.Status, taskType = t.TaskType, robotId = t.RobotId, mapVersionId = t.MapVersionId, createdAt = t.CreatedAt }).ToListAsync();
        return Ok(list);
    }

    [Authorize]
    [HttpGet("{taskId}")]
    public async Task<IActionResult> Get(Guid taskId, [FromServices] AppDbContext db)
    {
        var t = await db.Tasks.AsNoTracking().FirstOrDefaultAsync(x => x.TaskId == taskId);
        if (t == null) return NotFound();
        var dto = new { taskId = t.TaskId, status = t.Status, taskType = t.TaskType, robotId = t.RobotId, mapVersionId = t.MapVersionId, createdAt = t.CreatedAt, updatedAt = t.UpdatedAt, currentRouteId = t.CurrentRouteId, missionId = t.MissionId, parametersJson = t.ParametersJson };
        return Ok(dto);
    }

    [Authorize(Policy = BackendV2.Api.Infrastructure.Security.AuthorizationPolicies.Operator)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] TaskCreateRequest request, [FromServices] TaskManagerService tasks)
    {
        var actorId = User.FindFirst("sub")?.Value;
        var dto = await tasks.CreateAsync(request, Guid.TryParse(actorId, out var g) ? g : null);
        await using (var db = HttpContext.RequestServices.GetRequiredService<BackendV2.Api.Infrastructure.Persistence.AppDbContext>())
        {
            System.Guid? actor = System.Guid.TryParse(actorId, out var g2) ? g2 : null;
            await db.AuditEvents.AddAsync(new BackendV2.Api.Model.Ops.AuditEvent { AuditEventId = System.Guid.NewGuid(), Timestamp = System.DateTimeOffset.UtcNow, ActorUserId = actor, Action = "task.create", TargetType = "task", TargetId = dto.TaskId.ToString(), Outcome = "OK", DetailsJson = "{}" });
            await db.SaveChangesAsync();
        }
        return Ok(dto);
    }

    [Authorize(Policy = BackendV2.Api.Infrastructure.Security.AuthorizationPolicies.Operator)]
    [HttpPost("{taskId}/pause")]
    public async Task<IActionResult> Pause(Guid taskId, [FromServices] TaskManagerService tasks)
    {
        await tasks.ControlAsync(taskId, "pause");
        await using (var db = HttpContext.RequestServices.GetRequiredService<BackendV2.Api.Infrastructure.Persistence.AppDbContext>())
        {
            var actorId = User.FindFirst("sub")?.Value;
            System.Guid? actor = System.Guid.TryParse(actorId, out var g2) ? g2 : null;
            await db.AuditEvents.AddAsync(new BackendV2.Api.Model.Ops.AuditEvent { AuditEventId = System.Guid.NewGuid(), Timestamp = System.DateTimeOffset.UtcNow, ActorUserId = actor, Action = "task.pause", TargetType = "task", TargetId = taskId.ToString(), Outcome = "OK", DetailsJson = "{}" });
            await db.SaveChangesAsync();
        }
        return Ok(new { ok = true });
    }

    [Authorize(Policy = BackendV2.Api.Infrastructure.Security.AuthorizationPolicies.Operator)]
    [HttpPost("{taskId}/resume")]
    public async Task<IActionResult> Resume(Guid taskId, [FromServices] TaskManagerService tasks)
    {
        await tasks.ControlAsync(taskId, "resume");
        await using (var db = HttpContext.RequestServices.GetRequiredService<BackendV2.Api.Infrastructure.Persistence.AppDbContext>())
        {
            var actorId = User.FindFirst("sub")?.Value;
            System.Guid? actor = System.Guid.TryParse(actorId, out var g2) ? g2 : null;
            await db.AuditEvents.AddAsync(new BackendV2.Api.Model.Ops.AuditEvent { AuditEventId = System.Guid.NewGuid(), Timestamp = System.DateTimeOffset.UtcNow, ActorUserId = actor, Action = "task.resume", TargetType = "task", TargetId = taskId.ToString(), Outcome = "OK", DetailsJson = "{}" });
            await db.SaveChangesAsync();
        }
        return Ok(new { ok = true });
    }

    [Authorize(Policy = BackendV2.Api.Infrastructure.Security.AuthorizationPolicies.Operator)]
    [HttpPost("{taskId}/cancel")]
    public async Task<IActionResult> Cancel(Guid taskId, [FromServices] TaskManagerService tasks)
    {
        await tasks.ControlAsync(taskId, "cancel");
        await using (var db = HttpContext.RequestServices.GetRequiredService<BackendV2.Api.Infrastructure.Persistence.AppDbContext>())
        {
            var actorId = User.FindFirst("sub")?.Value;
            System.Guid? actor = System.Guid.TryParse(actorId, out var g2) ? g2 : null;
            await db.AuditEvents.AddAsync(new BackendV2.Api.Model.Ops.AuditEvent { AuditEventId = System.Guid.NewGuid(), Timestamp = System.DateTimeOffset.UtcNow, ActorUserId = actor, Action = "task.cancel", TargetType = "task", TargetId = taskId.ToString(), Outcome = "OK", DetailsJson = "{}" });
            await db.SaveChangesAsync();
        }
        return Ok(new { ok = true });
    }
}

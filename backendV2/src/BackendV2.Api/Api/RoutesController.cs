using System.Threading.Tasks;
using BackendV2.Api.Dto.Routes;
using BackendV2.Api.Mapping.Tasks;
using BackendV2.Api.Infrastructure.Persistence;
using BackendV2.Api.Service.Routes;
using BackendV2.Api.Topics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BackendV2.Api.Api;

[ApiController]
[Route("api/v1/routes")]
public class RoutesController : ControllerBase
{
[Authorize(Policy = BackendV2.Api.Infrastructure.Security.AuthorizationPolicies.Planner)]
[HttpPost("plan")]
    public async System.Threading.Tasks.Task<IActionResult> Plan([FromBody] RoutePlanRequest request, [FromServices] RoutePlannerService planner, [FromServices] AppDbContext db, [FromServices] Microsoft.AspNetCore.SignalR.IHubContext<BackendV2.Api.Hub.RealtimeHub> hub)
    {
        try
        {
            var (route, segments, eta) = await planner.PlanAsync(request);
            await db.Routes.AddAsync(route);
            await db.SaveChangesAsync();
            await hub.Clients.Group(BackendV2.Api.SignalR.RealtimeGroups.Robots).SendCoreAsync(SignalRTopics.RouteUpdated, new object[] { new { routeId = route.RouteId.ToString(), mapVersionId = route.MapVersionId.ToString() } }, System.Threading.CancellationToken.None);
            var actor = User.FindFirst("sub")?.Value;
            System.Guid? actorId = System.Guid.TryParse(actor, out var g) ? g : null;
            await db.AuditEvents.AddAsync(new BackendV2.Api.Model.Ops.AuditEvent { AuditEventId = System.Guid.NewGuid(), Timestamp = System.DateTimeOffset.UtcNow, ActorUserId = actorId, Action = "route.plan", TargetType = "route", TargetId = route.RouteId.ToString(), Outcome = "OK", DetailsJson = "{}" });
            await db.SaveChangesAsync();
            var dto = BackendV2.Api.Mapping.Tasks.TaskMapper.ToDto(route);
            return Ok(dto);
        }
        catch (InvalidOperationException ex) when (string.Equals(ex.Message, "Unreachable goal", StringComparison.Ordinal))
        {
            return UnprocessableEntity(new { error = "unreachable", message = "Goal cannot be reached with current map constraints" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = "invalid", message = ex.Message });
        }
    }

    [Authorize]
    [HttpGet("{routeId}")]
    public async System.Threading.Tasks.Task<IActionResult> Get(Guid routeId, [FromServices] AppDbContext db)
    {
        var r = await db.Routes.AsNoTracking().FirstOrDefaultAsync(x => x.RouteId == routeId);
        if (r == null) return NotFound();
        var dto = BackendV2.Api.Mapping.Tasks.TaskMapper.ToDto(r);
        return Ok(dto);
    }

    [Authorize]
    [HttpGet("{routeId}/eta")]
    public async System.Threading.Tasks.Task<IActionResult> GetEta(Guid routeId, [FromServices] AppDbContext db)
    {
        var r = await db.Routes.AsNoTracking().FirstOrDefaultAsync(x => x.RouteId == routeId);
        if (r == null || r.EstimatedArrivalTime == null) return NotFound();
        return Ok(new { routeId = r.RouteId, estimatedArrivalTime = r.EstimatedArrivalTime });
    }
}

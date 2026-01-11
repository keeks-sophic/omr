using System.Threading.Tasks;
using BackendV2.Api.Contracts.Commands;
using BackendV2.Api.Service.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackendV2.Api.Api;

[ApiController]
[Route("api/v1/robots/{robotId}/commands")]
public class RobotsCommandsController : ControllerBase
{
    [Authorize(Policy = BackendV2.Api.Infrastructure.Security.AuthorizationPolicies.Operator)]
    [HttpPost("grip")]
    public Task<IActionResult> Grip(string robotId, [FromBody] GripCommand cmd, [FromServices] NatsPublisherStub nats)
    {
        return Execute(async () => await nats.PublishGripCommandAsync(robotId, cmd));
    }

    [Authorize(Policy = BackendV2.Api.Infrastructure.Security.AuthorizationPolicies.Operator)]
    [HttpPost("hoist")]
    public Task<IActionResult> Hoist(string robotId, [FromBody] HoistCommand cmd, [FromServices] NatsPublisherStub nats)
    {
        return Execute(async () => await nats.PublishHoistCommandAsync(robotId, cmd));
    }

    [Authorize(Policy = BackendV2.Api.Infrastructure.Security.AuthorizationPolicies.Operator)]
    [HttpPost("telescope")]
    public async Task<IActionResult> Telescope(string robotId, [FromBody] TelescopeCommand cmd, [FromServices] NatsPublisherStub nats, [FromServices] BackendV2.Api.Infrastructure.Persistence.AppDbContext db)
    {
        var s = await db.RobotSessions.AsNoTracking().FirstOrDefaultAsync(x => x.RobotId == robotId);
        if (s != null)
        {
            try
            {
                var flags = System.Text.Json.JsonSerializer.Deserialize<BackendV2.Api.Dto.Robots.RobotFeatureFlagsDto>(s.FeatureFlagsJson) ?? new BackendV2.Api.Dto.Robots.RobotFeatureFlagsDto();
                if (!flags.TelescopeEnabled) return new ObjectResult(new { error = "feature_disabled", message = "telescope disabled" }) { StatusCode = 403 };
            }
            catch { }
        }
        return await Execute(async () => await nats.PublishTelescopeCommandAsync(robotId, cmd));
    }

    [Authorize(Policy = BackendV2.Api.Infrastructure.Security.AuthorizationPolicies.Operator)]
    [HttpPost("cam-toggle")]
    public Task<IActionResult> CamToggle(string robotId, [FromBody] CamToggleCommand cmd, [FromServices] NatsPublisherStub nats)
    {
        return Execute(async () => await nats.PublishCamToggleCommandAsync(robotId, cmd));
    }

    [Authorize(Policy = BackendV2.Api.Infrastructure.Security.AuthorizationPolicies.Operator)]
    [HttpPost("rotate")]
    public Task<IActionResult> Rotate(string robotId, [FromBody] RotateCommand cmd, [FromServices] NatsPublisherStub nats)
    {
        return Execute(async () => await nats.PublishRotateCommandAsync(robotId, cmd));
    }

    [Authorize(Policy = BackendV2.Api.Infrastructure.Security.AuthorizationPolicies.Operator)]
    [HttpPost("mode")]
    public Task<IActionResult> Mode(string robotId, [FromBody] ModeCommand cmd, [FromServices] NatsPublisherStub nats)
    {
        return Execute(async () => await nats.PublishModeCommandAsync(robotId, cmd));
    }

    private static async Task<IActionResult> Execute(System.Func<Task> f)
    {
        await f();
        return new OkObjectResult(new { ok = true });
    }
}

using System.IdentityModel.Tokens.Jwt;
using BackendV3.Endpoints;
using BackendV3.Infrastructure.Security;
using BackendV3.Modules.Robots.Dto;
using BackendV3.Modules.Robots.Dto.Requests;
using BackendV3.Modules.Robots.Mapping;
using BackendV3.Modules.Robots.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackendV3.Modules.Robots.Api;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.Viewer)]
public sealed class RobotsController : ControllerBase
{
    [HttpGet(ApiRoutes.Robots.Base)]
    public async Task<IActionResult> ListRobots(
        [FromServices] RobotRegistryService robots,
        CancellationToken ct)
    {
        var list = await robots.ListDtosAsync(ct);
        return Ok(list);
    }

    [HttpGet(ApiRoutes.Robots.ById)]
    public async Task<IActionResult> GetRobot(
        string robotId,
        [FromServices] RobotRegistryService robots,
        CancellationToken ct)
    {
        var dto = await robots.GetDtoAsync(robotId, ct);
        return dto == null ? NotFound() : Ok(dto);
    }

    [Authorize(Policy = AuthorizationPolicies.Operator)]
    [HttpPut(ApiRoutes.Robots.ById)]
    public async Task<IActionResult> UpdateRobot(
        string robotId,
        [FromBody] RobotUpdateRequest req,
        [FromServices] RobotRegistryService robots,
        [FromServices] RobotHubPublisher hub,
        CancellationToken ct)
    {
        var tagsJson = req.Tags?.RootElement.GetRawText();
        await robots.UpdateMetadataAsync(robotId, req.DisplayName, req.IsEnabled, tagsJson, req.Notes, ct);
        await hub.RobotMetaUpdatedAsync(robotId, ct);
        var dto = await robots.GetDtoAsync(robotId, ct);
        return dto == null ? NotFound() : Ok(dto);
    }

    [HttpGet(ApiRoutes.Robots.Identity)]
    public async Task<IActionResult> GetIdentity(
        string robotId,
        [FromServices] RobotRegistryService robots,
        [FromServices] RobotIdentityService identity,
        CancellationToken ct)
    {
        await robots.EnsureExistsAsync(robotId, ct);
        var snap = await identity.GetLatestAsync(robotId, ct);
        return snap == null ? NotFound() : Ok(RobotMapper.ToIdentityDto(snap));
    }

    [HttpGet(ApiRoutes.Robots.Capability)]
    public async Task<IActionResult> GetCapability(
        string robotId,
        [FromServices] RobotRegistryService robots,
        [FromServices] RobotCapabilityService capability,
        CancellationToken ct)
    {
        await robots.EnsureExistsAsync(robotId, ct);
        var snap = await capability.GetLatestAsync(robotId, ct);
        return snap == null ? NotFound() : Ok(RobotMapper.ToCapabilityDto(snap));
    }

    [HttpGet(ApiRoutes.Robots.SettingsReported)]
    public async Task<IActionResult> GetSettingsReported(
        string robotId,
        [FromServices] RobotRegistryService robots,
        [FromServices] RobotSettingsService settings,
        CancellationToken ct)
    {
        await robots.EnsureExistsAsync(robotId, ct);
        var snap = await settings.GetLatestReportedAsync(robotId, ct);
        return snap == null ? NotFound() : Ok(RobotMapper.ToSettingsReportedDto(snap));
    }

    [Authorize(Policy = AuthorizationPolicies.Operator)]
    [HttpPut(ApiRoutes.Robots.SettingsDesired)]
    public async Task<IActionResult> SetSettingsDesired(
        string robotId,
        [FromBody] RobotSettingsDesiredRequest req,
        [FromServices] RobotRegistryService robots,
        [FromServices] RobotSettingsService settings,
        CancellationToken ct)
    {
        await robots.EnsureExistsAsync(robotId, ct);
        await settings.PublishDesiredAsync(robotId, req.Payload);
        return Ok(new { ok = true });
    }

    [Authorize(Policy = AuthorizationPolicies.Operator)]
    [HttpPost(ApiRoutes.Robots.Commands)]
    public async Task<IActionResult> SendCommand(
        string robotId,
        [FromBody] RobotCommandRequest req,
        [FromServices] RobotRegistryService robots,
        [FromServices] RobotCommandService commands,
        CancellationToken ct)
    {
        await robots.EnsureExistsAsync(robotId, ct);
        var actor = GetActorUserId(User);
        var id = await commands.SendCommandAsync(robotId, req.CommandType, req.Payload, actor, ct);
        return Ok(new RobotCommandResponse { CommandId = id });
    }

    private static Guid? GetActorUserId(System.Security.Claims.ClaimsPrincipal user)
    {
        var sub = user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        return Guid.TryParse(sub, out var g) ? g : null;
    }
}


using System;
using System.Threading.Tasks;
using BackendV2.Api.Service.Tasks;
using BackendV2.Api.Contracts.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackendV2.Api.Api;

[ApiController]
[Route("api/v1/robots/{robotId}/commands")]
public class RobotCommandsController : ControllerBase
{
    [Authorize(Policy = BackendV2.Api.Infrastructure.Security.AuthorizationPolicies.Operator)]
    [HttpPost("grip")]
    public async Task<IActionResult> Grip(Guid robotId, [FromBody] GripCommand cmd, [FromServices] NatsPublisherStub publisher, [FromServices] BackendV2.Api.Infrastructure.Persistence.AppDbContext db)
    {
        var corr = await publisher.PublishGripCommandAsync(robotId.ToString(), cmd);
        await db.CommandOutbox.AddAsync(new BackendV2.Api.Model.Ops.CommandOutbox { OutboxId = Guid.NewGuid(), CorrelationId = corr, RobotId = robotId.ToString(), Subject = BackendV2.Api.Topics.NatsTopics.RobotCmd(robotId.ToString(), "grip"), PayloadJson = System.Text.Json.JsonSerializer.Serialize(cmd), CreatedAt = DateTimeOffset.UtcNow, LastAttempt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();
        return await Publish(robotId, corr, "robot.cmd.grip");
    }

    [Authorize(Policy = BackendV2.Api.Infrastructure.Security.AuthorizationPolicies.Operator)]
    [HttpPost("hoist")]
    public async Task<IActionResult> Hoist(Guid robotId, [FromBody] HoistCommand cmd, [FromServices] NatsPublisherStub publisher, [FromServices] BackendV2.Api.Infrastructure.Persistence.AppDbContext db)
    {
        var corr = await publisher.PublishHoistCommandAsync(robotId.ToString(), cmd);
        await db.CommandOutbox.AddAsync(new BackendV2.Api.Model.Ops.CommandOutbox { OutboxId = Guid.NewGuid(), CorrelationId = corr, RobotId = robotId.ToString(), Subject = BackendV2.Api.Topics.NatsTopics.RobotCmd(robotId.ToString(), "hoist"), PayloadJson = System.Text.Json.JsonSerializer.Serialize(cmd), CreatedAt = DateTimeOffset.UtcNow, LastAttempt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();
        return await Publish(robotId, corr, "robot.cmd.hoist");
    }

    [Authorize(Policy = BackendV2.Api.Infrastructure.Security.AuthorizationPolicies.Operator)]
    [HttpPost("telescope")]
    public async Task<IActionResult> Telescope(Guid robotId, [FromBody] TelescopeCommand cmd, [FromServices] NatsPublisherStub publisher, [FromServices] BackendV2.Api.Infrastructure.Persistence.AppDbContext db)
    {
        var corr = await publisher.PublishTelescopeCommandAsync(robotId.ToString(), cmd);
        await db.CommandOutbox.AddAsync(new BackendV2.Api.Model.Ops.CommandOutbox { OutboxId = Guid.NewGuid(), CorrelationId = corr, RobotId = robotId.ToString(), Subject = BackendV2.Api.Topics.NatsTopics.RobotCmd(robotId.ToString(), "telescope"), PayloadJson = System.Text.Json.JsonSerializer.Serialize(cmd), CreatedAt = DateTimeOffset.UtcNow, LastAttempt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();
        return await Publish(robotId, corr, "robot.cmd.telescope");
    }

    [Authorize(Policy = BackendV2.Api.Infrastructure.Security.AuthorizationPolicies.Operator)]
    [HttpPost("cam_toggle")]
    public async Task<IActionResult> CamToggle(Guid robotId, [FromBody] CamToggleCommand cmd, [FromServices] NatsPublisherStub publisher, [FromServices] BackendV2.Api.Infrastructure.Persistence.AppDbContext db)
    {
        var corr = await publisher.PublishCamToggleCommandAsync(robotId.ToString(), cmd);
        await db.CommandOutbox.AddAsync(new BackendV2.Api.Model.Ops.CommandOutbox { OutboxId = Guid.NewGuid(), CorrelationId = corr, RobotId = robotId.ToString(), Subject = BackendV2.Api.Topics.NatsTopics.RobotCmd(robotId.ToString(), "cam_toggle"), PayloadJson = System.Text.Json.JsonSerializer.Serialize(cmd), CreatedAt = DateTimeOffset.UtcNow, LastAttempt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();
        return await Publish(robotId, corr, "robot.cmd.cam_toggle");
    }

    [Authorize(Policy = BackendV2.Api.Infrastructure.Security.AuthorizationPolicies.Operator)]
    [HttpPost("rotate")]
    public async Task<IActionResult> Rotate(Guid robotId, [FromBody] RotateCommand cmd, [FromServices] NatsPublisherStub publisher, [FromServices] BackendV2.Api.Infrastructure.Persistence.AppDbContext db)
    {
        var corr = await publisher.PublishRotateCommandAsync(robotId.ToString(), cmd);
        await db.CommandOutbox.AddAsync(new BackendV2.Api.Model.Ops.CommandOutbox { OutboxId = Guid.NewGuid(), CorrelationId = corr, RobotId = robotId.ToString(), Subject = BackendV2.Api.Topics.NatsTopics.RobotCmd(robotId.ToString(), "rotate"), PayloadJson = System.Text.Json.JsonSerializer.Serialize(cmd), CreatedAt = DateTimeOffset.UtcNow, LastAttempt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();
        return await Publish(robotId, corr, "robot.cmd.rotate");
    }

    [Authorize(Policy = BackendV2.Api.Infrastructure.Security.AuthorizationPolicies.Operator)]
    [HttpPost("mode")]
    public async Task<IActionResult> Mode(Guid robotId, [FromBody] ModeCommand cmd, [FromServices] NatsPublisherStub publisher, [FromServices] BackendV2.Api.Infrastructure.Persistence.AppDbContext db)
    {
        var corr = await publisher.PublishModeCommandAsync(robotId.ToString(), cmd);
        await db.CommandOutbox.AddAsync(new BackendV2.Api.Model.Ops.CommandOutbox { OutboxId = Guid.NewGuid(), CorrelationId = corr, RobotId = robotId.ToString(), Subject = BackendV2.Api.Topics.NatsTopics.RobotCmd(robotId.ToString(), "mode"), PayloadJson = System.Text.Json.JsonSerializer.Serialize(cmd), CreatedAt = DateTimeOffset.UtcNow, LastAttempt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();
        return await Publish(robotId, corr, "robot.cmd.mode");
    }

    private async Task<IActionResult> Publish(Guid robotId, string correlationId, string action)
    {
        var actor = User.FindFirst("sub")?.Value;
        await using (var db = HttpContext.RequestServices.GetRequiredService<BackendV2.Api.Infrastructure.Persistence.AppDbContext>())
        {
            System.Guid? actorId = System.Guid.TryParse(actor, out var g) ? g : null;
            await db.AuditEvents.AddAsync(new BackendV2.Api.Model.Ops.AuditEvent { AuditEventId = System.Guid.NewGuid(), Timestamp = System.DateTimeOffset.UtcNow, ActorUserId = actorId, Action = action, TargetType = "robot", TargetId = robotId.ToString(), Outcome = "OK", DetailsJson = System.Text.Json.JsonSerializer.Serialize(new { correlationId }) });
            await db.SaveChangesAsync();
        }
        return Ok(new { ok = true, correlationId });
    }
}

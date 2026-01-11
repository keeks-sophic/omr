using System;
using System.Linq;
using System.Threading.Tasks;
using BackendV2.Api.Data.Auth;
using BackendV2.Api.Infrastructure.Persistence;
using BackendV2.Api.Infrastructure.Security;
using BackendV2.Api.Model.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BackendV2.Api.Api;

[ApiController]
[Route("api/v1/admin/users")]
public class AdminUsersController : ControllerBase
{
    [HttpGet("health")]
    public IActionResult Health() => Ok(new { ok = true });

    [Authorize(Policy = AuthorizationPolicies.Admin)]
    [HttpGet]
    public async Task<IActionResult> List([FromServices] UserRepository users, [FromServices] RoleRepository rolesRepo, [FromServices] AppDbContext db)
    {
        var list = await users.ListAsync();
        var dtos = await Task.WhenAll(list.Select(async u => new
        {
            userId = u.UserId,
            username = u.Username,
            displayName = u.DisplayName,
            isDisabled = u.IsDisabled,
            roles = await rolesRepo.GetUserRolesAsync(u.UserId)
        }));
        return Ok(dtos);
    }

    public class CreateUserRequest { public string Username { get; set; } = string.Empty; public string DisplayName { get; set; } = string.Empty; public string Password { get; set; } = string.Empty; public string[] Roles { get; set; } = Array.Empty<string>(); }

    [Authorize(Policy = AuthorizationPolicies.Admin)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest req, [FromServices] AppDbContext db, [FromServices] PasswordHasher hasher)
    {
        var u = new User { UserId = Guid.NewGuid(), Username = req.Username, DisplayName = req.DisplayName, PasswordHash = hasher.Hash(req.Password), IsDisabled = false };
        await db.Users.AddAsync(u);
        var roles = await db.Roles.Where(r => req.Roles.Contains(r.Name)).Select(r => r.RoleId).ToListAsync();
        foreach (var roleId in roles) await db.UserRoles.AddAsync(new UserRole { UserId = u.UserId, RoleId = roleId });
        await db.SaveChangesAsync();
        var actor = User.FindFirst("sub")?.Value;
        Guid? actorId = Guid.TryParse(actor, out var g) ? g : null;
        await db.AuditEvents.AddAsync(new BackendV2.Api.Model.Ops.AuditEvent { AuditEventId = Guid.NewGuid(), Timestamp = DateTimeOffset.UtcNow, ActorUserId = actorId, Action = "admin.user.create", TargetType = "auth.user", TargetId = u.UserId.ToString(), Outcome = "OK", DetailsJson = "{}" });
        await db.SaveChangesAsync();
        return Ok(new { userId = u.UserId });
    }

    public class UpdateUserRequest { public string DisplayName { get; set; } = string.Empty; public string? Password { get; set; } }

    [Authorize(Policy = AuthorizationPolicies.Admin)]
    [HttpPut("{userId}")]
    public async Task<IActionResult> Update(Guid userId, [FromBody] UpdateUserRequest req, [FromServices] AppDbContext db, [FromServices] PasswordHasher hasher)
    {
        var u = await db.Users.FirstOrDefaultAsync(x => x.UserId == userId);
        if (u == null) return NotFound();
        u.DisplayName = req.DisplayName;
        if (!string.IsNullOrWhiteSpace(req.Password)) u.PasswordHash = hasher.Hash(req.Password);
        await db.SaveChangesAsync();
        var actor = User.FindFirst("sub")?.Value;
        Guid? actorId = Guid.TryParse(actor, out var g) ? g : null;
        await db.AuditEvents.AddAsync(new BackendV2.Api.Model.Ops.AuditEvent { AuditEventId = Guid.NewGuid(), Timestamp = DateTimeOffset.UtcNow, ActorUserId = actorId, Action = "admin.user.update", TargetType = "auth.user", TargetId = userId.ToString(), Outcome = "OK", DetailsJson = "{}" });
        await db.SaveChangesAsync();
        return Ok(new { ok = true });
    }

    [Authorize(Policy = AuthorizationPolicies.Admin)]
    [HttpPost("{userId}/disable")]
    public async Task<IActionResult> Disable(Guid userId, [FromServices] AppDbContext db)
    {
        var u = await db.Users.FirstOrDefaultAsync(x => x.UserId == userId);
        if (u == null) return NotFound();
        u.IsDisabled = true;
        await db.SaveChangesAsync();
        var actor = User.FindFirst("sub")?.Value;
        Guid? actorId = Guid.TryParse(actor, out var g) ? g : null;
        await db.AuditEvents.AddAsync(new BackendV2.Api.Model.Ops.AuditEvent { AuditEventId = Guid.NewGuid(), Timestamp = DateTimeOffset.UtcNow, ActorUserId = actorId, Action = "admin.user.disable", TargetType = "auth.user", TargetId = userId.ToString(), Outcome = "OK", DetailsJson = "{}" });
        await db.SaveChangesAsync();
        return Ok(new { ok = true });
    }

    public class AssignRolesRequest { public string[] Roles { get; set; } = Array.Empty<string>(); }

    [Authorize(Policy = AuthorizationPolicies.Admin)]
    [HttpPost("{userId}/roles")]
    public async Task<IActionResult> AssignRoles(Guid userId, [FromBody] AssignRolesRequest req, [FromServices] AppDbContext db)
    {
        var u = await db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId);
        if (u == null) return NotFound();
        var current = await db.UserRoles.Where(x => x.UserId == userId).ToListAsync();
        db.UserRoles.RemoveRange(current);
        var roles = await db.Roles.Where(r => req.Roles.Contains(r.Name)).Select(r => r.RoleId).ToListAsync();
        foreach (var roleId in roles) await db.UserRoles.AddAsync(new UserRole { UserId = userId, RoleId = roleId });
        await db.SaveChangesAsync();
        var actor = User.FindFirst("sub")?.Value;
        Guid? actorId = Guid.TryParse(actor, out var g) ? g : null;
        await db.AuditEvents.AddAsync(new BackendV2.Api.Model.Ops.AuditEvent { AuditEventId = Guid.NewGuid(), Timestamp = DateTimeOffset.UtcNow, ActorUserId = actorId, Action = "admin.user.roles", TargetType = "auth.user", TargetId = userId.ToString(), Outcome = "OK", DetailsJson = "{}" });
        await db.SaveChangesAsync();
        return Ok(new { ok = true });
    }
}

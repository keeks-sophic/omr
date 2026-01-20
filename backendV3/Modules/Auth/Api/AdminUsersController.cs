using BackendV3.Endpoints;
using BackendV3.Infrastructure.Persistence;
using BackendV3.Infrastructure.Security;
using BackendV3.Modules.Auth.Data;
using BackendV3.Modules.Auth.Dto;
using BackendV3.Modules.Auth.Mapping;
using BackendV3.Modules.Auth.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BackendV3.Modules.Auth.Api;

[ApiController]
public sealed class AdminUsersController : ControllerBase
{
    [Authorize(Policy = AuthorizationPolicies.Admin)]
    [HttpGet(ApiRoutes.AdminUsers.Base)]
    public async Task<IActionResult> List(
        [FromServices] UserRepository users,
        [FromServices] RoleRepository rolesRepo,
        CancellationToken ct)
    {
        var list = await users.ListAsync(ct);
        var dtos = await Task.WhenAll(list.Select(async u =>
        {
            var roles = await rolesRepo.GetUserRolesAsync(u.UserId, ct);
            return UserMapper.ToDto(u, roles);
        }));

        return Ok(dtos);
    }

    [Authorize(Policy = AuthorizationPolicies.Admin)]
    [HttpGet(ApiRoutes.AdminUsers.ById)]
    public async Task<IActionResult> GetById(
        Guid userId,
        [FromServices] AppDbContext db,
        [FromServices] RoleRepository rolesRepo,
        CancellationToken ct)
    {
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId, ct);
        if (user == null) return NotFound();
        var roles = await rolesRepo.GetUserRolesAsync(userId, ct);
        return Ok(UserMapper.ToDto(user, roles));
    }

    [Authorize(Policy = AuthorizationPolicies.Admin)]
    [HttpPost(ApiRoutes.AdminUsers.Base)]
    public async Task<IActionResult> Create(
        [FromBody] CreateUserRequest req,
        [FromServices] AppDbContext db,
        [FromServices] PasswordHasher hasher,
        [FromServices] RoleRepository rolesRepo,
        CancellationToken ct)
    {
        var username = (req.Username ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(req.Password)) return BadRequest();

        var exists = await db.Users.AsNoTracking().AnyAsync(x => x.Username == username, ct);
        if (exists) return Conflict(new { error = "username_taken" });

        var now = DateTimeOffset.UtcNow;
        var user = new User
        {
            UserId = Guid.NewGuid(),
            Username = username,
            DisplayName = (req.DisplayName ?? string.Empty).Trim(),
            PasswordHash = hasher.Hash(req.Password),
            IsDisabled = false,
            CreatedAt = now,
            UpdatedAt = now
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        var roleIds = await rolesRepo.ResolveRoleIdsAsync(req.Roles ?? Array.Empty<string>(), ct);
        foreach (var roleId in roleIds)
        {
            db.UserRoles.Add(new UserRole { UserId = user.UserId, RoleId = roleId });
        }

        await db.SaveChangesAsync(ct);
        return Ok(new { userId = user.UserId });
    }

    [Authorize(Policy = AuthorizationPolicies.Admin)]
    [HttpPut(ApiRoutes.AdminUsers.ById)]
    public async Task<IActionResult> Update(
        Guid userId,
        [FromBody] UpdateUserRequest req,
        [FromServices] AppDbContext db,
        [FromServices] PasswordHasher hasher,
        CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(x => x.UserId == userId, ct);
        if (user == null) return NotFound();

        user.DisplayName = (req.DisplayName ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(req.Password))
        {
            user.PasswordHash = hasher.Hash(req.Password);
        }

        if (req.IsDisabled.HasValue)
        {
            user.IsDisabled = req.IsDisabled.Value;
        }

        user.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    [Authorize(Policy = AuthorizationPolicies.Admin)]
    [HttpPost(ApiRoutes.AdminUsers.Disable)]
    public async Task<IActionResult> Disable(
        Guid userId,
        [FromServices] AppDbContext db,
        CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(x => x.UserId == userId, ct);
        if (user == null) return NotFound();
        user.IsDisabled = true;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    [Authorize(Policy = AuthorizationPolicies.Admin)]
    [HttpPost(ApiRoutes.AdminUsers.Roles)]
    public async Task<IActionResult> AssignRoles(
        Guid userId,
        [FromBody] AssignRolesRequest req,
        [FromServices] AppDbContext db,
        [FromServices] RoleRepository rolesRepo,
        CancellationToken ct)
    {
        var exists = await db.Users.AsNoTracking().AnyAsync(x => x.UserId == userId, ct);
        if (!exists) return NotFound();

        var current = await db.UserRoles.Where(x => x.UserId == userId).ToListAsync(ct);
        db.UserRoles.RemoveRange(current);

        var roleIds = await rolesRepo.ResolveRoleIdsAsync(req.Roles ?? Array.Empty<string>(), ct);
        foreach (var roleId in roleIds)
        {
            db.UserRoles.Add(new UserRole { UserId = userId, RoleId = roleId });
        }

        await db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }
}

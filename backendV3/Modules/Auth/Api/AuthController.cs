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
using System.IdentityModel.Tokens.Jwt;

namespace BackendV3.Modules.Auth.Api;

[ApiController]
public sealed class AuthController : ControllerBase
{
    [AllowAnonymous]
    [HttpPost(ApiRoutes.Auth.Login)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        [FromServices] UserRepository users,
        [FromServices] RoleRepository rolesRepo,
        [FromServices] PasswordHasher hasher,
        [FromServices] JwtTokenService tokens,
        CancellationToken ct)
    {
        var uname = (request.Username ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(uname) || string.IsNullOrWhiteSpace(request.Password)) return Unauthorized();

        var user = await users.GetByUsernameAsync(uname, ct);
        if (user == null || user.IsDisabled || !hasher.Verify(request.Password, user.PasswordHash)) return Unauthorized();

        var roles = await rolesRepo.GetUserRolesAsync(user.UserId, ct);
        var (token, expiresAt, _) = tokens.CreateToken(user, roles);

        return Ok(new LoginResponse
        {
            AccessToken = token,
            ExpiresAt = expiresAt,
            User = UserMapper.ToDto(user, roles)
        });
    }

    [AllowAnonymous]
    [HttpPost(ApiRoutes.Auth.Register)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        [FromServices] AppDbContext db,
        [FromServices] PasswordHasher hasher,
        CancellationToken ct)
    {
        var username = (request.Username ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(request.Password)) return BadRequest(new { error = "missing_fields" });

        var exists = await db.Users.AsNoTracking().AnyAsync(x => x.Username == username, ct);
        if (exists) return Conflict(new { error = "username_taken" });

        var now = DateTimeOffset.UtcNow;
        var user = new User
        {
            UserId = Guid.NewGuid(),
            Username = username,
            DisplayName = (request.DisplayName ?? string.Empty).Trim(),
            PasswordHash = hasher.Hash(request.Password),
            IsDisabled = false,
            CreatedAt = now,
            UpdatedAt = now
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        var pendingRoleId = await db.Roles.Where(r => r.Name == AuthorizationPolicies.Pending).Select(r => r.RoleId).FirstOrDefaultAsync(ct);
        if (pendingRoleId == Guid.Empty)
        {
            var role = new Role { RoleId = Guid.NewGuid(), Name = AuthorizationPolicies.Pending };
            db.Roles.Add(role);
            await db.SaveChangesAsync(ct);
            pendingRoleId = role.RoleId;
        }

        db.UserRoles.Add(new UserRole { UserId = user.UserId, RoleId = pendingRoleId });
        await db.SaveChangesAsync(ct);

        return Ok(new { userId = user.UserId });
    }

    [Authorize(Policy = AuthorizationPolicies.AnyAuthenticated)]
    [HttpGet(ApiRoutes.Auth.Me)]
    public IActionResult Me()
    {
        var sub = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? "";
        var username = User.FindFirst(JwtRegisteredClaimNames.UniqueName)?.Value ?? User.Identity?.Name ?? "";
        var roles = User.FindAll("roles").Select(c => c.Value).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();

        return Ok(new
        {
            userId = sub,
            username,
            roles
        });
    }

    [Authorize(Policy = AuthorizationPolicies.AnyAuthenticated)]
    [HttpPost(ApiRoutes.Auth.Logout)]
    public async Task<IActionResult> Logout(
        [FromServices] TokenRepository tokens,
        [FromServices] JwtOptions jwtOptions,
        CancellationToken ct)
    {
        var sub = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        var jti = User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
        Guid? userId = Guid.TryParse(sub, out var g) ? g : null;

        if (string.IsNullOrWhiteSpace(jti)) return Ok(new { ok = true });

        await tokens.RevokeAsync(userId, jti, DateTimeOffset.UtcNow.AddMinutes(jwtOptions.ExpiryMinutes), ct);
        return Ok(new { ok = true });
    }
}

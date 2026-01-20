using System;
using System.Linq;
using System.Threading.Tasks;
using BackendV2.Api.Data.Auth;
using BackendV2.Api.Dto.Auth;
using BackendV2.Api.Infrastructure.Security;
using BackendV2.Api.Model.Ops;
using BackendV2.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BackendV2.Api.Api;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    [HttpGet("health")]
    public IActionResult Health() => Ok(new { ok = true });

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        [FromServices] UserRepository users,
        [FromServices] RoleRepository rolesRepo,
        [FromServices] AccessPolicyRepository policyRepo,
        [FromServices] PasswordHasher hasher,
        [FromServices] JwtTokenService tokens,
        [FromServices] AppDbContext db)
    {
        var uname = (request.Username ?? "").Trim();
        if (string.IsNullOrWhiteSpace(uname) || string.IsNullOrWhiteSpace(request.Password)) return Unauthorized();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == uname.ToLower());
        if (user == null || user.IsDisabled || !hasher.Verify(request.Password, user.PasswordHash))
        {
            await db.AuditEvents.AddAsync(new AuditEvent
            {
                AuditEventId = Guid.NewGuid(),
                Timestamp = DateTimeOffset.UtcNow,
                ActorUserId = null,
                Action = "auth.login",
                TargetType = "auth.user",
                TargetId = request.Username,
                Outcome = "DENIED",
                DetailsJson = "{}"
            });
            await db.SaveChangesAsync();
            return Unauthorized();
        }

        var roles = await rolesRepo.GetUserRolesAsync(user.UserId);
        var policy = await policyRepo.GetByUserAsync(user.UserId);
        var allowed = policy?.AllowedRobotIds;
        var (token, expiresAt) = tokens.CreateToken(user, roles, allowed);
        var (refreshToken, refreshExpiresAt) = tokens.CreateRefreshToken(user, roles, allowed);

        await db.AuditEvents.AddAsync(new AuditEvent
        {
            AuditEventId = Guid.NewGuid(),
            Timestamp = DateTimeOffset.UtcNow,
            ActorUserId = user.UserId,
            Action = "auth.login",
            TargetType = "auth.user",
            TargetId = user.UserId.ToString(),
            Outcome = "OK",
            DetailsJson = "{}"
        });
        await db.SaveChangesAsync();

        return Ok(new LoginResponse
        {
            AccessToken = token,
            ExpiresAt = expiresAt,
            RefreshToken = refreshToken,
            RefreshExpiresAt = refreshExpiresAt,
            User = new UserDto
            {
                UserId = user.UserId,
                Username = user.Username,
                DisplayName = user.DisplayName,
                Roles = roles,
                IsDisabled = user.IsDisabled
            }
        });
    }
    
    public class RegisterRequest { public string Username { get; set; } = string.Empty; public string DisplayName { get; set; } = string.Empty; public string Password { get; set; } = string.Empty; }
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req, [FromServices] AppDbContext db, [FromServices] PasswordHasher hasher)
    {
        var allowEnv = Environment.GetEnvironmentVariable("BACKENDV2_ALLOW_SIGNUP");
        var allow = string.IsNullOrEmpty(allowEnv) ? true : allowEnv.Equals("true", StringComparison.OrdinalIgnoreCase);
        if (!allow) return Unauthorized(new { error = "signup_disabled" });
        var uname = (req.Username ?? "").Trim();
        var display = (req.DisplayName ?? "").Trim();
        if (string.IsNullOrWhiteSpace(uname) || string.IsNullOrWhiteSpace(req.Password)) return BadRequest(new { error = "missing_fields" });
        var exists = await db.Users.AnyAsync(u => u.Username.ToLower() == uname.ToLower());
        if (exists) return Conflict(new { error = "username_taken" });
        var u = new BackendV2.Api.Model.Auth.User { UserId = Guid.NewGuid(), Username = uname, DisplayName = display, PasswordHash = hasher.Hash(req.Password), IsDisabled = false };
        await db.Users.AddAsync(u);
        var viewerRole = await db.Roles.FirstOrDefaultAsync(r => r.Name == "Viewer");
        if (viewerRole != null) await db.UserRoles.AddAsync(new BackendV2.Api.Model.Auth.UserRole { UserId = u.UserId, RoleId = viewerRole.RoleId });
        await db.SaveChangesAsync();
        await db.AuditEvents.AddAsync(new BackendV2.Api.Model.Ops.AuditEvent { AuditEventId = Guid.NewGuid(), Timestamp = DateTimeOffset.UtcNow, ActorUserId = u.UserId, Action = "auth.register", TargetType = "auth.user", TargetId = u.UserId.ToString(), Outcome = "OK", DetailsJson = "{}" });
        await db.SaveChangesAsync();
        return Ok(new { userId = u.UserId });
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        var userIdClaim = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var username = User.Identity?.Name ?? User.FindFirst("unique_name")?.Value ?? "";
        var roleClaims = System.Linq.Enumerable.ToList(User.FindAll("roles"));
        var rolesArr = roleClaims.Count > 0 ? System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(roleClaims, c => c.Value)) : (User.FindFirst("roles")?.Value ?? "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        var robotsCsv = User.FindFirst("allowedRobotIds")?.Value ?? "";
        return Ok(new
        {
            userId = userIdClaim,
            username,
            roles = rolesArr,
            allowedRobotIds = robotsCsv.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
        });
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromServices] AppDbContext db)
    {
        var sub = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
        var jti = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti)?.Value;
        Guid? actor = Guid.TryParse(sub, out var g) ? g : null;
        if (!string.IsNullOrEmpty(jti))
        {
            await db.RevokedTokens.AddAsync(new BackendV2.Api.Model.Auth.RevokedToken { RevocationId = Guid.NewGuid(), UserId = actor, Jti = jti, RevokedAt = DateTimeOffset.UtcNow, ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(60) });
            await db.SaveChangesAsync();
        }
        await db.AuditEvents.AddAsync(new BackendV2.Api.Model.Ops.AuditEvent { AuditEventId = Guid.NewGuid(), Timestamp = DateTimeOffset.UtcNow, ActorUserId = actor, Action = "auth.logout", TargetType = "auth.user", TargetId = actor?.ToString() ?? "", Outcome = "OK", DetailsJson = "{}" });
        await db.SaveChangesAsync();
        return Ok(new { ok = true });
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] BackendV2.Api.Dto.Auth.RefreshRequest request, [FromServices] AppDbContext db, [FromServices] JwtTokenService tokens)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken)) return BadRequest(new { error = "missing_refresh_token" });
        var jwtOptions = HttpContext.RequestServices.GetRequiredService<JwtOptions>();
        var key = Environment.GetEnvironmentVariable("BACKENDV2_JWTKEY") ?? jwtOptions.Key;
        var parameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.RefreshAudience,
            IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(key)),
            NameClaimType = "unique_name",
            RoleClaimType = "roles",
            ValidateLifetime = true,
            ClockSkew = System.TimeSpan.Zero
        };
        try
        {
            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(request.RefreshToken, parameters, out var validatedToken);
            var sub = principal.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
            var jti = principal.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti)?.Value;
            if (string.IsNullOrEmpty(sub) || !System.Guid.TryParse(sub, out var userId)) return Unauthorized();
            var user = await db.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null || user.IsDisabled) return Unauthorized();
            if (!string.IsNullOrEmpty(jti))
            {
                var revoked = await db.RevokedTokens.AsNoTracking().AnyAsync(x => x.Jti == jti);
                if (revoked) return Unauthorized();
            }
            var roleClaims = System.Linq.Enumerable.ToList(principal.FindAll("roles"));
            var roles = roleClaims.Count > 0 ? System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(roleClaims, c => c.Value)) : (principal.FindFirst("roles")?.Value ?? "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            var robotsCsv = principal.FindFirst("allowedRobotIds")?.Value ?? "";
            var allowed = robotsCsv.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            var (accessToken, accessExpires) = tokens.CreateToken(user, roles, allowed);
            var (newRefreshToken, refreshExpires) = tokens.CreateRefreshToken(user, roles, allowed);
            if (!string.IsNullOrEmpty(jti))
            {
                await db.RevokedTokens.AddAsync(new BackendV2.Api.Model.Auth.RevokedToken { RevocationId = Guid.NewGuid(), UserId = user.UserId, Jti = jti, RevokedAt = DateTimeOffset.UtcNow, ExpiresAt = refreshExpires });
                await db.SaveChangesAsync();
            }
            await db.AuditEvents.AddAsync(new BackendV2.Api.Model.Ops.AuditEvent { AuditEventId = Guid.NewGuid(), Timestamp = DateTimeOffset.UtcNow, ActorUserId = user.UserId, Action = "auth.refresh", TargetType = "auth.user", TargetId = user.UserId.ToString(), Outcome = "OK", DetailsJson = "{}" });
            await db.SaveChangesAsync();
            return Ok(new BackendV2.Api.Dto.Auth.RefreshResponse { AccessToken = accessToken, ExpiresAt = accessExpires, RefreshToken = newRefreshToken, RefreshExpiresAt = refreshExpires });
        }
        catch
        {
            await db.AuditEvents.AddAsync(new BackendV2.Api.Model.Ops.AuditEvent { AuditEventId = System.Guid.NewGuid(), Timestamp = System.DateTimeOffset.UtcNow, ActorUserId = null, Action = "auth.refresh", TargetType = "auth.user", TargetId = "", Outcome = "DENIED", DetailsJson = "{}" });
            await db.SaveChangesAsync();
            return Unauthorized();
        }
    }
}

using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BackendV2.Api.Model.Auth;
using Microsoft.IdentityModel.Tokens;

namespace BackendV2.Api.Infrastructure.Security;

public class JwtTokenService
{
    private readonly JwtOptions _options;
    public JwtTokenService(JwtOptions options) { _options = options; }

    public (string token, DateTimeOffset expiresAt) CreateToken(User user, string[] roles, string[]? allowedRobotIds)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("BACKENDV2_JWTKEY") ?? _options.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTimeOffset.UtcNow.AddMinutes(_options.ExpiryMinutes);
        var jti = Guid.NewGuid().ToString();
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
            new Claim(JwtRegisteredClaimNames.Jti, jti),
            new Claim("roles", string.Join(",", roles)),
            new Claim("allowedRobotIds", allowedRobotIds is { Length: > 0 } ? string.Join(",", allowedRobotIds) : "")
        };
        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expires.UtcDateTime,
            signingCredentials: creds
        );
        var encoded = new JwtSecurityTokenHandler().WriteToken(token);
        return (encoded, expires);
    }

    public (string token, DateTimeOffset expiresAt) CreateRefreshToken(User user, string[] roles, string[]? allowedRobotIds)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("BACKENDV2_JWTKEY") ?? _options.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTimeOffset.UtcNow.AddMinutes(_options.RefreshExpiryMinutes);
        var jti = Guid.NewGuid().ToString();
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
            new Claim(JwtRegisteredClaimNames.Jti, jti),
            new Claim("roles", string.Join(",", roles)),
            new Claim("allowedRobotIds", allowedRobotIds is { Length: > 0 } ? string.Join(",", allowedRobotIds) : "")
        };
        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.RefreshAudience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expires.UtcDateTime,
            signingCredentials: creds
        );
        var encoded = new JwtSecurityTokenHandler().WriteToken(token);
        return (encoded, expires);
    }
}

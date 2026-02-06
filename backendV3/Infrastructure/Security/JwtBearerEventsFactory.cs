using BackendV3.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;

namespace BackendV3.Infrastructure.Security;

public static class JwtBearerEventsFactory
{
    public static JwtBearerEvents Create()
    {
        return new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"].ToString();
                if (string.IsNullOrWhiteSpace(accessToken))
                {
                    accessToken = context.Request.Cookies["bk_at"];
                }

                if (!string.IsNullOrWhiteSpace(accessToken))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            },
            OnTokenValidated = async context =>
            {
                var jti = context.Principal?.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
                if (string.IsNullOrWhiteSpace(jti)) return;

                await using var scope = context.HttpContext.RequestServices.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var revoked = await db.RevokedTokens.AsNoTracking().AnyAsync(x => x.Jti == jti);
                if (revoked)
                {
                    context.Fail("token_revoked");
                }
            }
        };
    }
}

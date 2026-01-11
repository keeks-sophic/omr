using System;
using System.Threading.Tasks;
using BackendV2.Api.Infrastructure.Persistence;
using BackendV2.Api.Model.Ops;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.IdentityModel.Tokens.Jwt;

namespace BackendV2.Api.Infrastructure.Security;

public class AuthorizationAuditHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly IAuthorizationMiddlewareResultHandler _defaultHandler = new AuthorizationMiddlewareResultHandler();

    public async Task HandleAsync(RequestDelegate next, HttpContext context, AuthorizationPolicy policy, PolicyAuthorizationResult authorizeResult)
    {
        if (!authorizeResult.Succeeded)
        {
            var db = context.RequestServices.GetRequiredService<AppDbContext>();
            Guid? actor = null;
            var sub = context.User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            if (Guid.TryParse(sub, out var g)) actor = g;
            var path = context.Request.Path.Value ?? "";
            await db.AuditEvents.AddAsync(new AuditEvent { AuditEventId = Guid.NewGuid(), Timestamp = DateTimeOffset.UtcNow, ActorUserId = actor, Action = "authorization.denied", TargetType = "endpoint", TargetId = path, Outcome = "DENIED", DetailsJson = "{}" });
            await db.SaveChangesAsync();
        }
        await _defaultHandler.HandleAsync(next, context, policy, authorizeResult);
    }
}

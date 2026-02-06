using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Security.Claims;

namespace BackendV3.Infrastructure.Logging;

public sealed class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _log;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> log)
    {
        _next = next;
        _log = log;
    }

    public async Task Invoke(HttpContext ctx)
    {
        var sw = Stopwatch.StartNew();
        var correlationId = CorrelationIds.GetOrCreate(ctx);
        var endpoint = ctx.GetEndpoint();
        var routePattern = (endpoint as RouteEndpoint)?.RoutePattern?.RawText;
        var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? ctx.User.FindFirstValue("sub");
        var roles = ctx.User.FindAll("roles").Select(x => x.Value).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToArray();

        using (_log.BeginScope(new Dictionary<string, object?>
        {
            ["correlationId"] = correlationId,
            ["requestId"] = ctx.TraceIdentifier,
            ["method"] = ctx.Request.Method,
            ["path"] = ctx.Request.Path.Value,
            ["route"] = routePattern,
            ["userId"] = userId,
            ["roles"] = roles.Length == 0 ? null : string.Join(",", roles)
        }))
        {
            _log.LogInformation("{event} {method} {path}", LogEvents.Api.RequestStart, ctx.Request.Method, ctx.Request.Path.Value);
            try
            {
                await _next(ctx);
                _log.LogInformation("{event} {statusCode} {durationMs}", LogEvents.Api.RequestEnd, ctx.Response.StatusCode, sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "{event} {durationMs}", LogEvents.Api.RequestException, sw.ElapsedMilliseconds);
                throw;
            }
        }
    }
}


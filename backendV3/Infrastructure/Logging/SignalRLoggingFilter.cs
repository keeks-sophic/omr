using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using System.Diagnostics;
using System.Security.Claims;

namespace BackendV3.Infrastructure.Logging;

public sealed class SignalRLoggingFilter : IHubFilter
{
    public async ValueTask<object?> InvokeMethodAsync(HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object?>> next)
    {
        var sw = Stopwatch.StartNew();
        var http = invocationContext.Context.GetHttpContext();
        var correlationId = http != null ? CorrelationIds.GetOrCreate(http) : null;
        var userId = invocationContext.Context.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? invocationContext.Context.User?.FindFirstValue("sub");
        var roles = invocationContext.Context.User?.FindAll("roles").Select(x => x.Value).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToArray() ?? Array.Empty<string>();

        var log = Serilog.Log
            .ForContext("correlationId", correlationId)
            .ForContext("connectionId", invocationContext.Context.ConnectionId)
            .ForContext("hub", invocationContext.Hub.GetType().Name)
            .ForContext("method", invocationContext.HubMethodName)
            .ForContext("userId", userId)
            .ForContext("roles", roles.Length == 0 ? null : string.Join(",", roles));

        log.Information("{event} {hubMethod}", LogEvents.SignalR.InvocationStart, invocationContext.HubMethodName);
        try
        {
            var result = await next(invocationContext);
            log.Information("{event} {durationMs}", LogEvents.SignalR.InvocationEnd, sw.ElapsedMilliseconds);
            return result;
        }
        catch (Exception ex)
        {
            log.Error(ex, "{event} {durationMs}", LogEvents.SignalR.InvocationFailed, sw.ElapsedMilliseconds);
            throw;
        }
    }
}

using Microsoft.AspNetCore.Http;

namespace BackendV3.Infrastructure.Logging;

public static class CorrelationIds
{
    public const string HeaderName = "x-correlation-id";

    public static string GetOrCreate(HttpContext ctx)
    {
        if (ctx.Items.TryGetValue(HeaderName, out var existing) && existing is string s && !string.IsNullOrWhiteSpace(s))
        {
            return s;
        }

        var fromHeader = ctx.Request.Headers[HeaderName].ToString();
        var value = !string.IsNullOrWhiteSpace(fromHeader) ? fromHeader : Guid.NewGuid().ToString("N");
        ctx.Items[HeaderName] = value;
        ctx.Response.Headers[HeaderName] = value;
        return value;
    }
}


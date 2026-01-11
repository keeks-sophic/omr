using System;
using System.Collections.Concurrent;

namespace BackendV2.Api.Service.Ingestion;

public class TelemetryRateLimiter
{
    private readonly ConcurrentDictionary<string, DateTimeOffset> _last = new();
    public bool ShouldProcess(string key, TimeSpan minInterval)
    {
        var now = DateTimeOffset.UtcNow;
        var last = _last.GetOrAdd(key, now.AddMinutes(-1));
        if (now - last >= minInterval)
        {
            _last[key] = now;
            return true;
        }
        return false;
    }
}

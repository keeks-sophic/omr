using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace BackendV2.Api.Service.Realtime;

public class HubThrottle
{
    private readonly ConcurrentDictionary<string, DateTimeOffset> _lastSend = new();
    public bool CanSend(string connectionId, TimeSpan interval)
    {
        var now = DateTimeOffset.UtcNow;
        var last = _lastSend.GetOrAdd(connectionId, now.AddMinutes(-1));
        if (now - last >= interval)
        {
            _lastSend[connectionId] = now;
            return true;
        }
        return false;
    }
}

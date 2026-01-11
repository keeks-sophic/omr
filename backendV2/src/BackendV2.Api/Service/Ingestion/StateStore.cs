using System.Collections.Concurrent;

namespace BackendV2.Api.Service.Ingestion;

public class StateStore
{
    private readonly ConcurrentDictionary<string, string> _states = new();
    public void Set(string robotId, string state) => _states[robotId] = state;
    public bool TryGet(string robotId, out string state) => _states.TryGetValue(robotId, out state!);
}

using System.Threading.Tasks;
using BackendV2.Api.Data.Sim;

namespace BackendV2.Api.Service.Simulation;

public class SimulationOrchestrator
{
    private readonly SimSessionRepository _sim;
    public SimulationOrchestrator(SimSessionRepository sim) { _sim = sim; }
    public Task<object> HealthAsync() => Task.FromResult<object>(new { ok = true });
}

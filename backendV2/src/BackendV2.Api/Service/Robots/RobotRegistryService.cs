using System.Threading.Tasks;
using BackendV2.Api.Data.Core;

namespace BackendV2.Api.Service.Robots;

public class RobotRegistryService
{
    private readonly RobotRepository _robots;
    public RobotRegistryService(RobotRepository robots) { _robots = robots; }
    public Task<object> HealthAsync() => Task.FromResult<object>(new { ok = true });
}

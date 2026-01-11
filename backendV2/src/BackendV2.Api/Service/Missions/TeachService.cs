using System.Threading.Tasks;
using BackendV2.Api.Data.Task;

namespace BackendV2.Api.Service.Missions;

public class TeachService
{
    private readonly TeachSessionRepository _teach;
    public TeachService(TeachSessionRepository teach) { _teach = teach; }
    public Task<object> HealthAsync() => Task.FromResult<object>(new { ok = true });
}

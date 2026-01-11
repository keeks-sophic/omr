using System.Threading.Tasks;
using BackendV2.Api.Data.Map;

namespace BackendV2.Api.Service.Map;

public class MapManagementService
{
    private readonly MapRepository _maps;
    public MapManagementService(MapRepository maps) { _maps = maps; }
    public Task<object> HealthAsync() => Task.FromResult<object>(new { ok = true });
}

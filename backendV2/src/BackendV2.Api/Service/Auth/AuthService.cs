using System.Threading.Tasks;
using BackendV2.Api.Data.Auth;

namespace BackendV2.Api.Service.Auth;

public class AuthService
{
    private readonly UserRepository _users;
    public AuthService(UserRepository users) { _users = users; }
    public Task<object> HealthAsync() => Task.FromResult<object>(new { ok = true });
}

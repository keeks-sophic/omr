using System.Threading.Tasks;
using BackendV2.Api.Infrastructure.Persistence;
using BackendV2.Api.Model.Auth;
using Microsoft.EntityFrameworkCore;

namespace BackendV2.Api.Data.Auth;

public class AccessPolicyRepository
{
    private readonly AppDbContext _db;
    public AccessPolicyRepository(AppDbContext db) { _db = db; }
    public Task<UserAccessPolicy?> GetByUserAsync(System.Guid userId) =>
        _db.UserAccessPolicies.FirstOrDefaultAsync(x => x.UserId == userId);
}

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BackendV2.Api.Infrastructure.Persistence;
using BackendV2.Api.Model.Auth;
using Microsoft.EntityFrameworkCore;

namespace BackendV2.Api.Data.Auth;

public class RoleRepository
{
    private readonly AppDbContext _db;
    public RoleRepository(AppDbContext db) { _db = db; }
    public Task<List<Role>> ListAsync() => _db.Roles.ToListAsync();
    public Task<string[]> GetUserRolesAsync(System.Guid userId)
    {
        return _db.UserRoles.Where(x => x.UserId == userId)
            .Join(_db.Roles, ur => ur.RoleId, r => r.RoleId, (ur, r) => r.Name)
            .ToArrayAsync();
    }
}

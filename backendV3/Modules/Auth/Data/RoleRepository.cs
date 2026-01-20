using BackendV3.Infrastructure.Persistence;
using BackendV3.Modules.Auth.Model;
using Microsoft.EntityFrameworkCore;

namespace BackendV3.Modules.Auth.Data;

public sealed class RoleRepository
{
    private readonly AppDbContext _db;

    public RoleRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<List<Role>> ListAsync(CancellationToken ct = default) =>
        _db.Roles.AsNoTracking().OrderBy(x => x.Name).ToListAsync(ct);

    public Task<string[]> GetUserRolesAsync(Guid userId, CancellationToken ct = default)
    {
        return _db.UserRoles.Where(x => x.UserId == userId)
            .Join(_db.Roles.AsNoTracking(), ur => ur.RoleId, r => r.RoleId, (ur, r) => r.Name)
            .ToArrayAsync(ct);
    }

    public Task<List<Guid>> ResolveRoleIdsAsync(string[] roleNames, CancellationToken ct = default)
    {
        if (roleNames.Length == 0) return Task.FromResult(new List<Guid>());
        var names = roleNames.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToArray();
        return _db.Roles.Where(r => names.Contains(r.Name)).Select(r => r.RoleId).ToListAsync(ct);
    }
}

